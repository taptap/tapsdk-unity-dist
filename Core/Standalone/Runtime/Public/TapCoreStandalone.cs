using TapSDK.Core.Internal;
using TapSDK.Core.Internal.Init;
using UnityEngine;
using TapSDK.Core.Standalone.Internal;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone.Internal.Openlog;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone.Internal.Http;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Bean;
using System.Threading.Tasks;
using System;
using System.Threading;
using TapSDK.UI;
using System.Runtime.InteropServices;

namespace TapSDK.Core.Standalone
{
    /// <summary>
    /// Represents the standalone implementation of the TapCore SDK.
    /// </summary>
    public class TapCoreStandalone : ITapCorePlatform
    {
        internal static Prefs Prefs;
        internal static User User;
        internal static TapTapSdkOptions coreOptions;

        // client 信息是否匹配
        internal static bool isClientInfoMatched = true;

        internal static TapGatekeeper gatekeeperData = new TapGatekeeper();

        /// <summary>
        /// 保护 requestClientSetting 回调里"校验会话 + 执行副作用（心跳开关/磁盘缓存/
        /// 事件通知/状态机更新）"这一整段逻辑的原子性，避免只在回调入口检查一次会话号
        /// 之后，副作用和状态机更新仍然基于过期会话执行（TOCTOU）。
        ///
        /// 会话号本身直接读取 TapInitStateManager.CurrentSession，而不是自己另外维护
        /// 一套独立代际号——曾经这样做过，但 TapTapSDK.Init() 调用 SetInProgress()
        /// 与调用 platformWrapper.Init()（进而触发这里 generation 自增）在时间上并不是
        /// 同一个原子操作：旧请求可能持着旧的本地代际号通过自己的校验，而全局层面
        /// TapInitStateManager 的代际号已经因为新一次 Init() 而前进，导致旧请求的结果
        /// 仍被当作"当前会话"用 UpdateSuccess/UpdateFailed 落地。统一读取
        /// TapInitStateManager 的会话号，从根上消除两套代际号不同步的可能。
        /// </summary>
        private static readonly object sessionLock = new object();

        /// <summary>
        /// 本模块自己的请求代际号，只在 sessionLock 保护下更新和读取。TapInitStateManager
        /// 的全局代际号推进（SetInProgress()）用的是它自己内部的另一把锁，跟这里的
        /// sessionLock 不是同一把——单靠回调入口那次 IsSessionCurrent 检查，仍然可能在
        /// "检查通过"和"副作用真正落地"之间，被并发的新一次 Init() 抢先推进全局代际：
        /// UpdateSuccess/UpdateFailed 之后会被 TapInitStateManager 正确拒绝，但心跳开关/
        /// 磁盘缓存/事件通知这些副作用已经用旧会话的数据跑完了（Codex 审查发现）。
        /// 这里改成请求发起时在 sessionLock 内递增这个本地版本号，回调提交副作用前在
        /// 同一把锁内比对版本号是否仍是最新——用同一把锁把"新请求让旧请求过期"和
        /// "旧请求提交副作用"绑定成互斥的两件事，不会再交叉。
        /// </summary>
        private static long currentRequestVersion = 0;

        private readonly TapHttp tapHttp = TapHttp.NewBuilder("TapSDKCore", TapTapSDK.Version).Build();

        /// <summary>
        /// Initializes a new instance of the <see cref="TapCoreStandalone"/> class.
        /// </summary>
        public TapCoreStandalone()
        {
            // Instantiate modules
            User = new User();
            TapLoom.Initialize();
        }

        /// <summary>
        /// Initializes the TapCore SDK with the specified options.
        /// </summary>
        /// <param name="options">The TapCore SDK options.</param>
        public void Init(TapTapSdkOptions options)
        {
            Init(options, null);
        }

        /// <summary>
        /// Initializes the TapCore SDK with the specified core options and additional options.
        /// </summary>
        /// <param name="coreOption">The TapCore SDK core options.</param>
        /// <param name="otherOptions">Additional TapCore SDK options.</param>
        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            // TapTapSDK.Init() 在委托给这里之前已经调用过 SetInProgress()，这里直接读取
            // 它返回的同一个会话号（而不是自己再另外维护一套独立代际号）
            long session = TapInitStateManager.CurrentSession;

            // 参数校验必须保持硬失败，不参与"只有 ConfigError 才判失败"的放宽：
            // 这两个分支是在 coreOptions 被赋值（见下方 coreOptions = coreOption）之<b>前</b>
            // 就 return 的，此时 coreOptions 还是 null。一旦状态机报成功，CheckInitState()
            // 会放行后续业务接口，那些接口读 coreOptions 会直接抛 NullReferenceException
            // ——不是"请求失败"，是崩溃。gatekeeper 的网络错误是可恢复的、配置本身有效，
            // 与这里性质完全不同。
            if (coreOption.clientId == null || coreOption.clientId.Length == 0)
            {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("clientId 不能为空", "clientId 不能为空");
                TapInitStateManager.UpdateFailed(session, TapInitErrorCode.ParamError, "clientId 不能为空");
                return;
            }
            if (coreOption.clientToken == null || coreOption.clientToken.Length == 0)
            {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("clientToken 不能为空", "clientToken 不能为空");
                TapInitStateManager.UpdateFailed(session, TapInitErrorCode.ParamError, "clientToken 不能为空");
                return;
            }

            // clientToken 是敏感凭证，序列化整个 coreOption 会把它原样打进日志；启用日志
            // 时这条 SDK Init Options 记录就会带着凭证明文，脱敏后再打印（Codex 审查发现）。
            string coreOptionJson = JsonConvert.SerializeObject(coreOption);
            if (!string.IsNullOrEmpty(coreOption.clientToken))
            {
                coreOptionJson = coreOptionJson.Replace(coreOption.clientToken, "***");
            }
            TapLog.Log("SDK Init Options : ", "coreOption : " + coreOptionJson + "\notherOptions : " + JsonConvert.SerializeObject(otherOptions));
            coreOptions = coreOption;
            // 设置区域与语言
            TapLocalizeManager.SetCurrentRegion(coreOption.region == TapTapRegionType.CN);
            TapLocalizeManager.SetCurrentLanguage(coreOption.preferredLanguage);
            if (Prefs == null)
            {
                Prefs = new Prefs();
            }
            TapOpenlogStandalone.Init();

            var path = Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + "_" + coreOption.clientId + ".json");
            // 兼容旧版文件
            if (!File.Exists(path))
            {
                var oldPath = Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + ".json");
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, path);
                }
            }
            if (File.Exists(path))
            {
                var clientSettings = File.ReadAllText(path);
                // TapLog.Log("本地 clientSettings: " + clientSettings);
                try
                {
                    TapGatekeeper tapGatekeeper = JsonConvert.DeserializeObject<TapGatekeeper>(clientSettings);
                    if (tapGatekeeper.Switch?.Heartbeat == true)
                    {
                        TapAppDurationStandalone.Enable();
                    }
                    else
                    {
                        TapAppDurationStandalone.Disable();
                    }
                    gatekeeperData = tapGatekeeper;
                }
                catch (System.Exception e)
                {
                    TapLog.Warning("TriggerEvent error: " + e.Message);
                }
            }

            long requestVersion;
            lock (sessionLock)
            {
                requestVersion = ++currentRequestVersion;
            }
            requestClientSetting(session, requestVersion);
        }

        public void UpdateLanguage(TapTapLanguageType language)
        {
            if (coreOptions == null)
            {
                TapLog.Log("coreOptions is null");
                return;
            }
            TapLog.Log("UpdateLanguage called with language: " + language);
            coreOptions.preferredLanguage = language;
            TapLocalizeManager.SetCurrentLanguage(language);
        }

        public static string getGatekeeperConfigUrl(string key)
        {
            if (gatekeeperData != null)
            {
                var urlsData = gatekeeperData.Urls;
                if (urlsData != null && urlsData.ContainsKey(key))
                {
                    var keyData = urlsData[key];
                    if (keyData != null)
                    {
                        return keyData.Browser;
                    }
                }
            }
            return null;
        }

        private void requestClientSetting(long session, long requestVersion)
        {
            // 使用 httpclient 请求 /sdk-core/v1/gatekeeper 获取配置
#if UNITY_EDITOR
            var bundleIdentifier = PlayerSettings.applicationIdentifier;
#else
            var bundleIdentifier = Application.identifier;
#endif
            var path = "sdk-core/v1/gatekeeper";
            var body = new Dictionary<string, object> {
                { "platform", "pc" },
                { "bundle_id", bundleIdentifier }
            };

            // gatekeeper 最多尝试 4 次（1 次首发 + 3 次重试）
            var retryStrategy = TapHttpRetryStrategy.CreateDefault(TapHttpBackoffStrategy.CreateExponentialLimited(maxRetryCount: 3));

            tapHttp.PostJson<TapGatekeeper>(
               url: path,
               json: body,
               retryStrategy: retryStrategy,
               onSuccess: (data) =>
               {
                   lock (sessionLock)
                   {
                       // requestVersion 的比对和上面 Init() 里的递增共享同一把 sessionLock，
                       // 两者互斥：不会出现"这里检查通过之后，新 Init() 才递增版本号"的窗口。
                       // IsSessionCurrent 保留作为第二层防御（两者语义上应该总是一致）。
                       if (requestVersion != currentRequestVersion || !TapInitStateManager.IsSessionCurrent(session))
                       {
                           // 已经有更新的 Init() 会话开始，这是过期会话的结果，整体丢弃
                           // （包括磁盘缓存写入、事件通知），避免污染新会话状态
                           return;
                       }
                       // gatekeeper 网络请求本身已经成功——按设计这就是 Success 的唯一
                       // 判定标准。心跳开关/磁盘缓存写入/事件通知都是附属操作，任何一步
                       // 抛异常（例如缓存目录不可写、JSON 序列化失败、事件监听器自身抛
                       // 异常）都不能阻止状态机进入 Success、不能让 UpdateSuccess 永远
                       // 不被调用，否则状态机会永久停留在 InProgress。只记录日志，不影响
                       // 下面的终态落地。
                       try
                       {
                           if (data.Switch?.Heartbeat == true)
                           {
                               TapAppDurationStandalone.Enable();
                           }
                           else
                           {
                               TapAppDurationStandalone.Disable();
                           }
                           gatekeeperData = data;
                           // 把 data 存储在本地
                           saveClientSettings(data);
                           // 发通知
                           EventManager.TriggerEvent(Constants.ClientSettingsEventKey, data);
                       }
                       catch (Exception e)
                       {
                           TapLog.Error("Init success side effect failed", e.Message);
                       }
                       TapInitStateManager.UpdateSuccess(session);
                   }
               },
               onFailure: (error) =>
               {
                   lock (sessionLock)
                   {
                       if (requestVersion != currentRequestVersion || !TapInitStateManager.IsSessionCurrent(session))
                       {
                           // 已经有更新的 Init() 会话开始，这是过期会话的结果，整体丢弃
                           return;
                       }
                       if (error is TapHttpServerException se && TapHttpErrorConstants.ERROR_INVALID_CLIENT.Equals(se.ErrorData.Error))
                       {
                           isClientInfoMatched = false;
                           // 这两行都是附属的通知副作用（打日志、弹提示），任何一步抛异常
                           // （例如 se.ErrorData.Msg 为 null 导致 ShowMessage 内部出错）都不能
                           // 阻止下面的 UpdateFailed 落地，否则状态机会永久卡在 InProgress，
                           // 所有已注册回调再也收不到任何通知（Greptile 审查发现）
                           try
                           {
                               TapLog.Error("Init Failed", se.ErrorData.ErrorDescription);
                               TapMessage.ShowMessage(se.ErrorData.Msg, TapMessage.Position.bottom, TapMessage.Time.twoSecond);
                           }
                           catch (Exception sideEffectException)
                           {
                               TapLog.Error("Init failed side effect threw", sideEffectException.Message);
                           }
                           TapInitStateManager.UpdateFailed(session, TapInitErrorCode.ConfigError, se.ErrorData.Msg ?? se.ErrorData.ErrorDescription ?? "invalid_client");
                       }
                       else
                       {
                           // 与 Android / iOS 对齐：只有 invalid_client 落 ConfigError Failed；
                           // gatekeeper 网络失败降级为 Success，沿用磁盘缓存或默认配置。
                           TapLog.Error("Init failed with recoverable error, treated as degraded success",
                               error?.Message ?? "network error");
                           TapInitStateManager.UpdateSuccess(session);
                       }
                   }
               }
           );
        }

        private void saveClientSettings(TapGatekeeper settings)
        {
            string json = JsonConvert.SerializeObject(settings);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + "_" + TapTapSDK.taptapSdkOptions.clientId + ".json"), json);
        }


        public static bool CheckInitState()
        {
            return CheckInitState(true);
        }

        /// <param name="showUI">为 false 时只做状态判断，不弹窗。供 SDK 内部后台初始化使用。</param>
        public static bool CheckInitState(bool showUI)
        {
            if (TapInitStateManager.TryGetNonSuccessMessage(out string shortMsg, out string detailMsg))
            {
                if (showUI)
                {
                    TapVerifyInitStateUtils.ShowVerifyErrorMsg(shortMsg, detailMsg);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// SDK 内部使用：本地同步初始化是否已开始。InProgress 视为通过，不要求 gatekeeper 成功。
        /// </summary>
        internal static bool CheckLocalInitState(bool showUI = true)
        {
            if (TapInitStateManager.TryGetLocalUnavailableMessage(out string shortMsg, out string detailMsg))
            {
                if (showUI)
                {
                    TapVerifyInitStateUtils.ShowVerifyErrorMsg(shortMsg, detailMsg);
                }
                return false;
            }
            return true;
        }

        // 获取当前用户设置的 DB userID
        public static string GetCurrentUserId()
        {
            return User?.Id;
        }


        // <summary>
        // 校验游戏是否通过启动器唤起，建立与启动器通讯
        //</summary>
        public async Task<bool> IsLaunchedFromTapTapPC()
        {
#if UNITY_STANDALONE_WIN
            return await TapClientStandalone.IsLaunchedFromTapTapPC();
#else
            throw new System.NotImplementedException();
#endif
        }

        public void SendOpenLog(
            string project,
            string version,
            string action,
            Dictionary<string, string> properties
        )
        {
            TapOpenlogStandalone.LogBusiness(project, version, action, properties);
        }

#if UNITY_STANDALONE_WIN
        public void RegisterTapTapPCStateChangeListener(Action<int> action)
        {
            TapClientStandalone.RegisterTapTapPCStateChangeListener(action);
        }

        public void UnRegisterTapTapPCStateChangeListener(Action<int> action)
        {
            TapClientStandalone.UnRegisterTapTapPCStateChangeListener(action);
        }
#endif
 }


    public interface IOpenIDProvider
    {
        string GetOpenID();
    }
}
