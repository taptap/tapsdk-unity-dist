using System;
using System.Threading.Tasks;
using System.Linq;
using TapSDK.Core.Internal;
using System.Collections.Generic;

using UnityEngine;
using System.Reflection;
using TapSDK.Core.Internal.Init;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;
using System.ComponentModel;

namespace TapSDK.Core {
    public class TapTapSDK {
        public static readonly string Version = "4.10.9";
        
        public static string SDKPlatform = "TapSDK-Unity";

        public static TapTapSdkOptions taptapSdkOptions;

        private static ITapCorePlatform platformWrapper;

        private static bool disableDurationStatistics;

        public static bool DisableDurationStatistics {
            get => disableDurationStatistics;
            set {
                disableDurationStatistics = value;
            }
        }

        static TapTapSDK() {
            platformWrapper = PlatformTypeUtils.CreatePlatformImplementationObject(typeof(ITapCorePlatform),
                "TapSDK.Core") as ITapCorePlatform;
        }

        /// <summary>
        /// 初始化 SDK。必须在主线程调用，且不支持从多个线程并发调用——SetInProgress()
        /// 与 platformWrapper.Init() 之间的会话归属假设只在主线程单次调用下成立
        /// （Codex 审查发现），并发调用可能让新旧会话的原生初始化结果互相污染。
        /// </summary>
        /// <param name="coreOption">核心库配置</param>
        public static void Init(TapTapSdkOptions coreOption)
        {
            AssertMainThread();
            if (coreOption == null)
            {
                const string message = "[TapSDK] options is null!";
                long failedSession = TapInitStateManager.SetInProgress();
                // 参数校验保持硬失败，不参与"只有 ConfigError 才判失败"的放宽：紧接着就会抛
                // ArgumentException，若状态机反而报成功，就会出现"抛异常 + 状态成功"的自相
                // 矛盾，且重复初始化时会推进会话号却继续用旧配置。
                TapInitStateManager.UpdateFailed(failedSession, TapInitErrorCode.ParamError, message);
                throw new ArgumentException(message);
            }
            long session = TapInitStateManager.SetInProgress();
            // platformWrapper.Init() 会异步（Mobile 原生回调 / Standalone 网络请求）报告
            // 终态，可能比下面的 IInitTask 循环、TapTapEvent.Init 这些同步步骤还快；标记
            // "同步初始化体正在跑"，让提前到达的原生结果先缓冲，等同步体真正跑完再决定
            // 是否发布，避免先收到成功、随后同步初始化才失败的不一致（Codex 审查发现）
            TapInitStateManager.BeginSyncInit(session);
            try
            {
                TapTapSDK.taptapSdkOptions = coreOption;
                TapLog.Enabled = coreOption.enableLog;
                // platformWrapper 为 null（反射创建平台实现失败、程序集被裁剪、条件编译
                // 缺少对应平台包等）时，?. 会静默跳过整段平台初始化；后面的同步步骤仍会
                // "正常"跑完，但唯一能产生终态的网络/原生路径永远不会执行，状态机会
                // 永久停留在 InProgress，回调也永远不会触发（Codex 审查发现）。必须显式
                // 报错，让下面已有的 catch 块统一落地 InternalError。
                if (platformWrapper == null)
                {
                    throw new InvalidOperationException("TapTapSDK platform implementation not found");
                }
                platformWrapper.Init(coreOption);
                // 初始化各个模块

                Type[] initTaskTypes = GetInitTypeList();
                if (initTaskTypes != null)
                {
                    List<IInitTask> initTasks = new List<IInitTask>();
                    foreach (Type initTaskType in initTaskTypes)
                    {
                        initTasks.Add(Activator.CreateInstance(initTaskType) as IInitTask);
                    }
                    initTasks = initTasks.OrderBy(task => task.Order).ToList();
                    foreach (IInitTask task in initTasks)
                    {
                        TapLogger.Debug($"Init: {task.GetType().Name}");
                        task.Init(coreOption);
                    }
                }
                TapTapEvent.Init(HandleEventOptions(null));
                TapInitStateManager.EndSyncInitSuccess(session);
            }
            catch (Exception e)
            {
                // 上面这些同步步骤（反射创建 IInitTask、模块初始化等）任何一步抛异常，都不能让
                // 状态机永久停留在 InProgress——否则 CheckInitState 永远失败，回调也永远不会
                // 触发。先落地一个明确终态、通知回调，再把异常抛给调用方。这些异常不是参数
                // 校验失败，用 InternalError 区分，避免误导接入方去检查 TapTapSdkOptions
                // 参数（Codex 审查发现：此前和上面 coreOption == null 的参数错误共用同一个
                // ParamError）
                // 同步初始化异常同样不参与"只有 ConfigError 才判失败"的放宽：那条放宽只针对
                // gatekeeper / 原生异步返回的可恢复错误。这里模块的同步初始化已经失败、异常也
                // 会抛给调用方，落 Success 会让业务接口在半初始化状态下被放行。
                TapLog.Error(e);
                TapInitStateManager.EndSyncInitFailed(session, TapInitErrorCode.InternalError, e.Message ?? "unknown error");
                throw;
            }
        }

        /// <summary>
        /// 初始化 SDK。必须在主线程调用，且不支持从多个线程并发调用，理由同上一个重载。
        /// </summary>
        /// <param name="coreOption">核心库配置</param>
        /// <param name="otherOptions">其他库配置</param>
        public static void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            AssertMainThread();
            if (coreOption == null)
            {
                const string message = "[TapSDK] options is null!";
                long failedSession = TapInitStateManager.SetInProgress();
                // 参数校验保持硬失败，不参与"只有 ConfigError 才判失败"的放宽：紧接着就会抛
                // ArgumentException，若状态机反而报成功，就会出现"抛异常 + 状态成功"的自相
                // 矛盾，且重复初始化时会推进会话号却继续用旧配置。
                TapInitStateManager.UpdateFailed(failedSession, TapInitErrorCode.ParamError, message);
                throw new ArgumentException(message);
            }

            long session = TapInitStateManager.SetInProgress();
            // 理由同上一个 Init 重载：先标记同步初始化体正在跑，缓冲提前到达的原生结果
            TapInitStateManager.BeginSyncInit(session);
            try
            {
                TapTapSDK.taptapSdkOptions = coreOption;
                TapLog.Enabled = coreOption.enableLog;
                // 理由同上一个 Init 重载：platformWrapper 为 null 时不能静默跳过。
                if (platformWrapper == null)
                {
                    throw new InvalidOperationException("TapTapSDK platform implementation not found");
                }
                platformWrapper.Init(coreOption, otherOptions);

                Type[] initTaskTypes = GetInitTypeList();
                if (initTaskTypes != null)
                {
                    List<IInitTask> initTasks = new List<IInitTask>();
                    foreach (Type initTaskType in initTaskTypes)
                    {
                        initTasks.Add(Activator.CreateInstance(initTaskType) as IInitTask);
                    }
                    initTasks = initTasks.OrderBy(task => task.Order).ToList();
                    foreach (IInitTask task in initTasks)
                    {
                        TapLog.Log($"Init: {task.GetType().Name}");
                        task.Init(coreOption, otherOptions);
                    }
                }
                TapTapEvent.Init(HandleEventOptions(otherOptions));
                TapInitStateManager.EndSyncInitSuccess(session);
            }
            catch (Exception e)
            {
                // 理由同上一个 Init 重载：同步失败路径也必须落入明确终态，再把异常抛给调用方；
                // 这些异常同样不是参数校验失败，用 InternalError 区分。
                // 同步初始化异常同样不参与"只有 ConfigError 才判失败"的放宽：那条放宽只针对
                // gatekeeper / 原生异步返回的可恢复错误。这里模块的同步初始化已经失败、异常也
                // 会抛给调用方，落 Success 会让业务接口在半初始化状态下被放行。
                TapLog.Error(e);
                TapInitStateManager.EndSyncInitFailed(session, TapInitErrorCode.InternalError, e.Message ?? "unknown error");
                throw;
            }
        }

        /// <summary>
        /// 通过初始化属性设置 TapEvent 属性，兼容旧版本
        /// </summary>
        /// <param name="coreOption"></param>
        /// <param name="otherOptions"></param>
        /// <returns>TapEvent 属性</returns>
        private static TapTapEventOptions HandleEventOptions(
            TapTapSdkBaseOptions[] otherOptions = null
        )
        {
            TapTapEventOptions tapEventOptions = null;
            if (otherOptions != null && otherOptions.Length > 0)
            {
                foreach (TapTapSdkBaseOptions otherOption in otherOptions)
                {
                    if (otherOption is TapTapEventOptions option)
                    {
                        tapEventOptions = option;
                    }
                }
            }
            if (tapEventOptions == null)
            {
                tapEventOptions = new TapTapEventOptions();
            }
            return tapEventOptions;
        }

        // UpdateLanguage 方法
        public static void UpdateLanguage(TapTapLanguageType language)
        {
            platformWrapper?.UpdateLanguage(language);
        }
        
        // 是否通过 PC 启动器唤起游戏
        public static Task<bool> IsLaunchedFromTapTapPC()
        {
            return platformWrapper?.IsLaunchedFromTapTapPC();
        }

#if UNITY_STANDALONE_WIN
        /// <summary>
        /// 注册 TapTap PC 客户端运行状态监听
        /// </summary>
        /// <param name="action">监听回调</param>
        public static void RegisterTapTapPCStateChangeListener(Action<int> action)
        {
            platformWrapper?.RegisterTapTapPCStateChangeListener(action);
        }

        /// <summary>
        /// 移除 TapTap PC 客户端运行状态监听
        /// </summary>
        /// <param name="action">监听回调</param>
        public static void UnRegisterTapTapPCStateChangeListener(Action<int> action)
        {
            platformWrapper?.UnRegisterTapTapPCStateChangeListener(action);
        }
#endif

        /// <summary>
        /// 把"必须在主线程调用、不支持并发调用"这条约束落实成运行时检查，放在两个 Init
        /// 入口最前面、SetInProgress() 之前。之前只有 Mobile 平台实现（TapCoreMobile）
        /// 自己做了这个检查，Standalone 完全没有校验；SetInProgress() 本身用锁保证了会话
        /// 号递增的原子性，但 TapCoreStandalone.Init() 之后会再自己重新读一次
        /// TapInitStateManager.CurrentSession，不是直接用调用方传下来的会话号——如果
        /// 两个线程并发调用 TapTapSDK.Init()，读到的可能已经是另一个线程刚推进的更新
        /// 会话号，旧调用的请求被错误绑定到新会话上，taptapSdkOptions 这个静态字段也会被
        /// 相互覆盖（Codex 审查发现：这条约束应该在两个公开入口统一校验，不能只靠某一个
        /// 平台实现各自为营）。Init() 全程是同步方法、没有 await 让出线程，只要都在主
        /// 线程调用，天然就是串行的，不需要再加锁。
        /// </summary>
        private static void AssertMainThread()
        {
            if (TapLoom.IsMainThreadKnown && !TapLoom.IsMainThread)
            {
                throw new InvalidOperationException(
                    "TapTapSDK.Init 必须在主线程调用，不支持从其它线程调用"
                );
            }
        }

        private static Type[] GetInitTypeList(){
            Type interfaceType = typeof(IInitTask);
            Type[] initTaskTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asssembly => asssembly.GetName().FullName.StartsWith("TapSDK"))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(clazz => interfaceType.IsAssignableFrom(clazz) && clazz.IsClass)
                .ToArray();
            return initTaskTypes;
        }

        /// <summary>
        /// 注册初始化结果回调。可以在 Init 之前或之后的任意时机调用；
        /// 若调用时初始化已经有结果，会尽快在主线程异步回调一次（并非同步，调用返回后结果不一定已经可用）。对同一个 callback 重复调用
        /// 本方法不会重复加入注册表，但只要当前已有结果，每次调用仍会重新补发一次；
        /// 如果不希望重复收到通知，请只在真正需要注册时调用一次。
        /// </summary>
        public static void AddInitCallback(ITapInitCallback callback)
        {
            TapInitStateManager.AddCallback(callback);
        }

        /// <summary>
        /// 注销初始化结果回调。注意：内部会在真正派发前再检查一次该 callback 是否仍在
        /// 注册表中，因此注销通常能取消掉已经排队但尚未真正执行的通知；但如果注销恰好
        /// 发生在"检查通过之后、真正调用之前"的极窄区间内，仍可能收到一次通知，这一点
        /// 无法完全杜绝。
        /// </summary>
        public static void RemoveInitCallback(ITapInitCallback callback)
        {
            TapInitStateManager.RemoveCallback(callback);
        }

        public static void SendOpenLog(
            string project,
            string version,
            string action,
            Dictionary<string, string> properties = null
        )
            {
                platformWrapper.SendOpenLog(project, version, action, properties);
        }

    }
}
