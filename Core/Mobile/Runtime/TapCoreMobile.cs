using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapSDK.Core.Internal;
using TapSDK.Core.Internal.Init;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using UnityEngine;

namespace TapSDK.Core.Mobile
{
    /// <summary>
    /// 原生 addInitCallback 结果里真正有意义的那三个值（剥掉桥接分发层的外层包装之后）。
    /// </summary>
    internal struct InitCallbackPayload
    {
        /// <summary>初始化结果码，0 表示成功。</summary>
        internal int code;

        /// <summary>失败原因，成功时为 null。</summary>
        internal string message;

        /// <summary>这次结果产生时的原生会话号（字符串形式）。</summary>
        internal string session;

        /// <summary>
        /// 外层 result 是桥接分发层包出来的：code 固定 0、message 固定 "Success"、content 里
        /// 装着原生 handler 自己序列化的那层 {code,message,content}。这里优先按新协议解析内层，
        /// 拿到真正的结果码与会话号。
        ///
        /// 内层解析不出来时回退成"把外层当作最终结果"：原生 SDK 若还是不包这一层的旧版本，
        /// content 就直接是会话号，此时外层 code 也确实代表初始化结果。这样新旧两种原生版本
        /// 都能工作，不会因为协议不匹配把终态整个丢掉、让状态机永久停在 InProgress。
        /// </summary>
        internal static InitCallbackPayload Parse(Result outer)
        {
            string raw = outer?.content;
            if (!string.IsNullOrEmpty(raw) && raw.TrimStart().StartsWith("{"))
            {
                try
                {
                    Result inner = new Result(raw);
                    // JsonUtility 对结构不匹配的 JSON 不会抛异常，只会留下默认值。用 content
                    // 是否解析出内容来判断这层到底是不是我们要的那层 —— 否则一个恰好以 '{'
                    // 开头但结构不同的字符串会被误当成内层，把会话号读成空。
                    if (!string.IsNullOrEmpty(inner.content))
                    {
                        return new InitCallbackPayload
                        {
                            code = inner.code,
                            message = inner.message,
                            session = inner.content,
                        };
                    }
                }
                catch (Exception e)
                {
                    // 只记日志、走下面的回退，不让一条畸形结果把整次初始化卡死
                    TapLog.Error(
                        "TapCoreMobile parse native init callback payload failed",
                        $"raw={raw}, error={e.Message}"
                    );
                }
            }
            return new InitCallbackPayload
            {
                code = outer?.code ?? Result.RESULT_ERROR,
                message = outer?.message,
                session = raw,
            };
        }
    }

    public class TapCoreMobile : ITapCorePlatform
    {
        private EngineBridge Bridge = EngineBridge.GetInstance();

        public TapCoreMobile()
        {
            TapLog.Log("TapCoreMobile constructor");
            TapLoom.Initialize();
            EngineBridgeInitializer.Initialize();
            // 由于当通过 Application.Quit 退出时，iOS 端不会收到 applicationWillTerminate 的通知，
            // 所以不会调用 C++ 的 OnAppStop 方法，导致小概率会因 C++ 资源未正确释放触发崩溃，所以添加监听
#if UNITY_IOS
            EventManager.AddListener(
                EventManager.OnApplicationQuit,
                (quit) =>
                {
                    TapLog.Log("TapSDK Unity OnApplicationQuit");
                    Bridge.CallHandler(
                        EngineBridgeInitializer
                            .GetBridgeServer()
                            .Method("handleEngineQuitEvent")
                            .CommandBuilder()
                    );
                }
            );
#endif
        }

        /// <summary>
        /// 原生 init 桥接命令同步返回时分配到的会话号；原生 addInitCallback 结果里携带的
        /// 会话号需要与它一致才会被采纳，用于丢弃属于更早一次 Init() 调用的过期原生结果。
        /// -1 表示未知/不做过滤（例如原生 SDK 版本还没有升级到会同步返回会话号）。
        /// </summary>
        private long expectedNativeSession = -1;

        /// <summary>
        /// expectedNativeSession 归属的 Unity 会话号（TapInitStateManager 的 generation）。
        /// 必须和 expectedNativeSession 一起原子更新、一起读取，不能在回调里重新读一次
        /// 当时的 TapInitStateManager.CurrentSession——如果这次 Init() 调用期间又发生了
        /// 更新一次 Init()，CurrentSession 会先被 SetInProgress() 推进到新会话，而这次原生
        /// 结果的回调可能在这之后才到达；如果回调临时重新读 CurrentSession，就会把这次
        /// 原生结果错误地归属到还没被原生 init 真正接受的新会话上（Codex 审查发现）。
        /// </summary>
        private long expectedUnitySession = -1;

        /// <summary>
        /// 只在降级模式（原生 SDK 不支持会话号，expectedNativeSession 恒为 -1）下使用：
        /// 记录当前还有多少次 Init() 调用尚未等到匹配的原生结果。
        /// </summary>
        private int pendingDegradedNativeInitCount = 0;

        /// <summary>
        /// 只在降级模式下使用：曾经同时有多次 Init() 调用在等待原生结果（原生结果不携带
        /// 任何可验证身份的信息，无法确认乱序到达的结果分别属于哪一次调用）。一旦进入
        /// 这个状态，在所有已发出的调用都被消耗完（pendingDegradedNativeInitCount 归零）
        /// 之前，任何到达的结果都不可信——不能只看"当前 pending 数量是不是刚好等于 1"，
        /// 因为如果两次调用的结果乱序到达（后发的先到），先到的那个会先把计数减到 1，
        /// 随后真正的旧结果到达时也会看到 pending == 1 而被误采纳（Greptile 审查发现）。
        /// </summary>
        private bool degradedModeAmbiguous = false;

        /// <summary>
        /// 保护 expectedNativeSession/expectedUnitySession/pendingDegradedNativeInitCount/
        /// degradedModeAmbiguous 这一整套会话配对状态。Init() 只强制约束调用方必须在
        /// 主线程发起，但原生端触发终态回调的那一刻未必与 Unity 主线程严格同步——iOS
        /// 通过 dispatch_get_main_queue 派发、Android 通过 TapLoom.QueueOnMainThread
        /// 封送，两者理论上都落在 Unity 自身运行的同一条线程上，但不同桥接实现各自的
        /// 派发细节并不是这里应该依赖的不变量。直接加锁保护这几个字段的读写，不再依赖
        /// "两边一定跑在同一线程"这个跨两套原生桥接代码才能验证的假设（Greptile 审查
        /// 发现）。
        /// </summary>
        private readonly object sessionLock = new object();

        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            // 主线程校验已经上移到 TapTapSDK.Init()（两个公开入口共用），在
            // platformWrapper.Init() 派发到这里之前就已经拦下非主线程调用，不需要在
            // 每个平台实现里各自重复一遍（Codex 审查发现：这条约束应该在公开入口统一
            // 校验，不能只靠某个平台实现各自为营）。
            TapLog.Log("TapCoreMobile SDK inited");
            SetPlatformAndVersion(TapTapSDK.SDKPlatform, TapTapSDK.Version);
            string coreOptionsJson = JsonUtility.ToJson(coreOption);
            string[] otherOptionsJson = otherOptions
                .Select(option => JsonConvert.SerializeObject(option))
                .ToArray();
            // 原生 initWithSession 桥接命令同步返回这次调用被分配到的原生会话号。
            // Android 侧特意没有直接改 init 命令本身的返回值（会改变已发布方法的
            // JVM 二进制签名，Codex 审查发现），而是新增了 initWithSession 独立命令；
            // iOS 侧按参数名而不是按命令名分发，这次改名不影响 iOS 落到同一个已经
            // 支持返回会话号的原生实现上。
            string sessionResult = Bridge.CallWithReturnValue(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("initWithSession")
                    .Args("coreOption", coreOptionsJson)
                    .Args("otherOptions", otherOptionsJson)
                    .CommandBuilder()
            );
            long nativeSession;
            if (!long.TryParse(sessionResult, out nativeSession))
            {
                // 解析失败（例如原生 SDK 版本还没有升级到会同步返回会话号）时不做会话
                // 过滤，退化为旧行为，避免因为拿不到会话号而彻底丢弃所有原生结果
                nativeSession = -1;
            }
            // 这一对值描述"这次原生 init 调用的结果应该归属于哪个 Unity 会话"，必须一起
            // 原子更新：跟原生终态回调那边读取这一整套状态共用同一把 sessionLock，
            // 不依赖"两边一定跑在同一线程"（Greptile 审查发现）。
            lock (sessionLock)
            {
                expectedNativeSession = nativeSession;
                expectedUnitySession = TapInitStateManager.CurrentSession;
                if (nativeSession < 0)
                {
                    if (pendingDegradedNativeInitCount > 0)
                    {
                        // 已经有一次降级模式的 Init() 调用还没等到结果，现在又开始新一次——
                        // 之后到达的任何结果都无法确认到底属于哪一次调用，进入不可信状态，
                        // 直到所有已发出的调用都被消耗完才能重新开始信任。
                        degradedModeAmbiguous = true;
                    }
                    pendingDegradedNativeInitCount++;
                }
            }
            RegisterNativeInitCallback();
        }

        private bool nativeInitCallbackRegistered = false;

        /// <summary>
        /// 注册一个持久（非 OnceTime）的桥接回调，转发原生端 addInitCallback 的结果到
        /// TapInitStateManager。原生端可能多次调用这个回调（晚注册补发 / 重新 init），
        /// 所以这里不设 OnceTime，只在整个应用生命周期注册一次。
        ///
        /// addInitCallback 是本次 PR 新增的原生桥接方法：如果集成方引用的 Android AAR /
        /// iOS Pod 还停留在不包含这个方法的旧版本，不会导致这里永远等不到结果——两端现有
        /// 的桥接分发基础设施（Android EngineBridge.execCommandInternal 的
        /// runCatching{}.onFailure{}，iOS TapSDKBridge.m 里包这层分发的 @try/@catch）
        /// 在反射/运行时找不到目标方法时都已经统一走「捕获异常→构造 code=-1 的错误结果→
        /// 通过回调派发」这条路径，不是抛到调用方或者直接吞掉不回调。下面 result.code == 0
        /// 的判断会把这个 -1 当成失败，走 UpdateFailed，不会让本次 Init() 停留在
        /// InProgress（Codex 审查发现，需要确认原生依赖版本落后时不会永久卡死；经过验证
        /// 两端现有基础设施已经保证这一点，见 EngineBridge.kt:execCommandInternal /
        /// TapSDKBridge.m 对应分支）。仍然建议：接入方引用的原生 SDK 版本需要包含本次
        /// PR 的桥接改动才能拿到真正的 gatekeeper 结果，而不是这条兜底失败路径。
        /// </summary>
        private void RegisterNativeInitCallback()
        {
            if (nativeInitCallbackRegistered)
            {
                return;
            }
            // 必须在 Bridge.CallHandler 真正成功注册之后才置位。如果提前置位，
            // CallHandler 同步抛异常时注册并没有真正生效，但下一次 Init() 调用会因为
            // 这个标记已经是 true 而永久跳过注册，原生初始化回调之后再也不会转发给
            // Unity（Codex 审查发现）。
            try
            {
                Bridge.CallHandler(
                    EngineBridgeInitializer
                        .GetBridgeServer()
                        .Method("addInitCallback")
                        .Callback(true)
                        .CommandBuilder(),
                    result =>
                    {
                        // 会话配对状态的读取、降级计数的读取与修改、以及依据它们做出的
                        // 采纳/丢弃决定，整体在同一把 sessionLock 里完成，不依赖"这个
                        // 回调和 Init() 一定跑在同一线程"（Greptile 审查发现）。
                        lock (sessionLock)
                        {
                        // 一次性把这一对值读进本地变量，而不是分别读 expectedNativeSession 一次、
                        // 后面再读 expectedUnitySession 一次：两次读取之间理论上可能被下一次
                        // Init() 更新成另一对新值，先读到旧的 nativeSession 通过校验、后读到已经
                        // 被换成新值的 unitySession，还是会导致误归属（Codex 审查发现的同一类
                        // 问题，这里在读取侧也要保证成对）。
                        long currentExpectedNativeSession = expectedNativeSession;
                        long currentExpectedUnitySession = expectedUnitySession;

                        // 原生 addInitCallback 的结果经过了<b>两层</b>包装，必须先剥掉外层：
                        //   ① 原生 handler 自己包一层 {code,message,content}，因为它要在会话号
                        //      之外再带回初始化结果码（iOS BridgeCoreService.m 里手工拼这段 JSON、
                        //      Android BridgeCoreServiceImpl.kt 用 TapJson 序列化 EngineBridgeResult）；
                        //   ② 桥接分发层又把整串当作 content 包进外层 Result 发给 Unity
                        //      （iOS TapSDKBridge.m 的 bridgeCallback、Android EngineBridge 同理）。
                        // 所以外层 result.content 是内层那串 JSON，不是会话号；外层 code 也只是
                        // 桥接分发是否成功（固定 0），不是初始化结果。
                        //
                        // 对比一下就知道这里为什么容易看错：同一次 Init 里的 initWithSession 走的是
                        // callWithReturnValue，拿到的是原生 handler 的<b>直接返回值</b>（裸会话号），
                        // 没有被包装 —— 同步返回值和异步回调这两条路径的包装行为本来就不一致。
                        InitCallbackPayload payload = InitCallbackPayload.Parse(result);
                        // 与 Init() 时同步记录的 expectedNativeSession 比对：如果原生端的旧结果
                        // 消息在 Unity 又调用了新一次 Init() 之后才被处理，会话号会不一致，
                        // 在这里就丢弃，不会走到下面的 UpdateSuccess/UpdateFailed。
                        if (currentExpectedNativeSession >= 0)
                        {
                            // expectedNativeSession >= 0 说明原生端已经是支持会话号的新协议，
                            // content 理应总能解析出会话号；解析失败（畸形/缺失）和会话号不匹配
                            // 一样可疑，都当作污染结果丢弃，而不是放行当作属于当前会话
                            // （Codex 审查发现：此前解析失败会被误当作当前会话直接采纳）
                            if (!long.TryParse(payload.session, out long resultSession) ||
                                resultSession != currentExpectedNativeSession)
                            {
                                TapLog.Error(
                                    "TapCoreMobile native init callback session invalid",
                                    $"expected={currentExpectedNativeSession}, session={payload.session}, raw={result.content}"
                                );
                                return;
                            }
                        }
                        else
                        {
                            // 降级模式：原生结果完全不携带任何可验证身份的信息。先记录消费
                            // 前的状态（是否已经处于不可信状态、消费前还有几个在等待），再
                            // 消费掉这一次；只有"消费前从未出现过重叠等待、且消费前刚好只有
                            // 这一个在等待"才能确认这个结果就是它产生的。乱序到达时，先到的
                            // 结果会让 pendingDegradedNativeInitCount 提前减到 1，但
                            // degradedModeAmbiguous 只有等到计数真正归零才会清除，所以随后
                            // 到达的旧结果仍会被这里挡住，不会被误采纳（Greptile 审查发现）。
                            bool wasAmbiguous = degradedModeAmbiguous;
                            int remainingPending = pendingDegradedNativeInitCount;
                            pendingDegradedNativeInitCount = Math.Max(0, pendingDegradedNativeInitCount - 1);
                            bool isLastPending = pendingDegradedNativeInitCount == 0;
                            if (isLastPending)
                            {
                                degradedModeAmbiguous = false;
                            }
                            if (wasAmbiguous || remainingPending != 1)
                            {
                                TapLog.Error(
                                    "TapCoreMobile native init callback ambiguous in degraded mode",
                                    $"pendingCount={remainingPending}, wasAmbiguous={wasAmbiguous}"
                                );
                                if (isLastPending)
                                {
                                    // 这是本轮重叠调用里最后一个还在等待的降级模式原生结果——
                                    // 不会再有更多结果到达了。如果只是像上面一样丢弃就直接
                                    // return，没有任何路径会再调用 UpdateSuccess/UpdateFailed，
                                    // 最新 Unity 会话会永久停留在 InProgress，CheckInitState()
                                    // 和已注册的 ITapInitCallback 都永远拿不到终态（Greptile
                                    // 审查发现：旧版原生 SDK 不携带会话号，两次 Init() 重叠且
                                    // 结果乱序到达时会连续丢弃两个终态）。这里没有办法确认这个
                                    // 结果到底归属哪一次调用，但至少要给当前会话落地一个终态，
                                    // 保证状态机不会永久卡死。
                                    //
                                    // 这里<b>不能</b>依据 payload 的内容来决定终态——进入这个分支的
                                    // 前提（wasAmbiguous || remainingPending != 1）就是"无法确认这个
                                    // 结果属于哪一次 Init()"。用一个归属不明的 code 去更新最新会话，
                                    // 两个方向都会错：旧会话的 ConfigError 后到会把最新那次合法的
                                    // 初始化判成失败；反过来最新那次的 ConfigError 也可能被当成
                                    // 可恢复错误放行（Codex 审查发现）。
                                    //
                                    // 所以落一个确定性的 Failed(InternalError)：结果不可信时保守拦住，
                                    // 而不是拿它冒险。这也不违反"只有 ConfigError 才判失败"的口径——
                                    // 那条放宽针对的是"能确认归属、且可恢复"的错误，而这里连归属都
                                    // 不成立，属于 Unity 侧自身的不确定性，不在放宽范围内。
                                    //
                                    // 要从根上去掉这个分支，只能让接入方引用带会话号的新版原生 SDK
                                    // （此时 expectedNativeSession >= 0，走上面那条精确校验的路径）。
                                    TapLog.Error(
                                        "TapCoreMobile degraded mode cannot attribute native init result, fail conservatively",
                                        $"旧版原生 SDK 不支持区分重叠 Init() 调用的结果归属；本次结果 code={payload.code}, message={payload.message}"
                                    );
                                    TapInitStateManager.UpdateFailed(
                                        currentExpectedUnitySession,
                                        TapInitErrorCode.InternalError,
                                        "旧版原生 SDK 不支持区分重叠 Init() 调用的结果归属，无法确认最新一次调用的结果"
                                    );
                                }
                                return;
                            }
                        }
                        // 用 Init() 当时记录的 expectedUnitySession，而不是重新读一次
                        // TapInitStateManager.CurrentSession——如果这次结果到达前又有更新的
                        // Init() 调用把全局会话号推进了，重新读会把这个旧结果错误地归属到
                        // 尚未被原生 init 真正接受的新会话上（Codex 审查发现）。
                        long session = currentExpectedUnitySession;
                        // 用内层的 code / message：外层那对值是桥接分发的结果（code 固定 0、
                        // message 固定 "Success"），拿它判断会让 onInitFail 永远走不到失败分支，
                        // 真实的 errorCode / errorMsg 也全部丢失。
                        //
                        // 只有 ConfigError（1001，gatekeeper 返回 invalid_client，应用信息与
                        // clientId/clientToken 不匹配、不可自动恢复）才落 Failed，其余错误暂时按
                        // 成功处理。
                        //
                        // 这条放宽只适用于「原生 / gatekeeper 异步返回的初始化结果」，不覆盖
                        // 参数校验和同步初始化异常——那两类是调用前置条件不满足，配置本身还没
                        // 建立起来，放成 Success 会让业务接口拿着 null 配置跑。
                        //
                        // 注意口径：这不等于"与线上完全一致"。线上（iOS TapTapSDK.m 的
                        // checkInitState）把非 ConfigError 的 Failed 归为 INIT_STATE_EMPTY，
                        // 业务接口同样会被拦，只是提示文案更温和；这里判成 Success 会放行接口，
                        // 比线上更宽松。取这个方向是为了不让新引入的状态机比线上更严格地拦住
                        // 可恢复错误（TryGetNonSuccessMessage 只放行 Success）。
                        // 错误本身照旧记日志，不静默吞掉。
                        if (payload.code == 0)
                        {
                            TapInitStateManager.UpdateSuccess(session);
                        }
                        else if (payload.code == TapInitErrorCode.ConfigError)
                        {
                            TapInitStateManager.UpdateFailed(session, payload.code, payload.message);
                        }
                        else
                        {
                            TapLog.Error(
                                "TapCoreMobile native init failed with recoverable error, treated as success for now",
                                $"code={payload.code}, message={payload.message}"
                            );
                            TapInitStateManager.UpdateSuccess(session);
                        }
                        }
                    }
                );
                nativeInitCallbackRegistered = true;
            }
            catch (Exception)
            {
                nativeInitCallbackRegistered = false;
                throw;
            }
        }

        private void SetPlatformAndVersion(string platform, string version)
        {
            TapLog.Log(
                "TapCoreMobile SetPlatformAndVersion called with platform: "
                    + platform
                    + " and version: "
                    + version
            );
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("setPlatformAndVersion")
                    .Args("platform", TapTapSDK.SDKPlatform)
                    .Args("version", TapTapSDK.Version)
                    .CommandBuilder()
            );
            SetSDKArtifact("Unity");
        }

        private void SetSDKArtifact(string value)
        {
            TapLog.Log("TapCoreMobile SetSDKArtifact called with value: " + value);
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("setSDKArtifact")
                    .Args("artifact", "Unity")
                    .CommandBuilder()
            );
        }

        public void Init(TapTapSdkOptions coreOption)
        {
            Init(coreOption, new TapTapSdkBaseOptions[0]);
        }

        public void UpdateLanguage(TapTapLanguageType language)
        {
            TapLog.Log("TapCoreMobile UpdateLanguage language: " + language);
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("updateLanguage")
                    .Args("language", (int)language)
                    .CommandBuilder()
            );
        }

        public Task<bool> IsLaunchedFromTapTapPC()
        {
            return Task.FromResult(false);
        }

        public void SendOpenLog(
            string project,
            string version,
            string action,
            Dictionary<string, string> properties
        )
        {
            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }
            string propertiesJson = JsonConvert.SerializeObject(properties);
            Bridge.CallHandler(
                EngineBridgeInitializer
                    .GetBridgeServer()
                    .Method("sendOpenLog")
                    .Args("project", project)
                    .Args("version", version)
                    .Args("action", action)
                    .Args("properties", propertiesJson)
                    .CommandBuilder()
            );
        }
    }
}
