using System;
using System.Collections.Generic;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Core.Internal.Init
{
    internal enum TapInitState
    {
        Idle,
        InProgress,
        Success,
        Failed
    }

    /// <summary>
    /// 平台无关的初始化状态机，Mobile（Android/iOS 桥接结果）和 Standalone
    /// （自建 gatekeeper 请求结果）都调用这里的方法上报结果。
    /// Success 只能来自本次会话 gatekeeper 网络请求的真实成功。
    /// </summary>
    internal static class TapInitStateManager
    {
        private static readonly object Lock = new object();

        private static TapInitState state = TapInitState.Idle;
        private static int errorCode;
        private static string errorMsg;

        private static readonly List<ITapInitCallback> callbacks = new List<ITapInitCallback>();

        /// <summary>
        /// 全局会话代际号：每次 SetInProgress 递增一次。UpdateSuccess/UpdateFailed 和
        /// AddCallback 的终态补发都通过 TapLoom 异步切到主线程才真正执行回调；这里记录
        /// 每次通知排队时的代际号，真正执行前再校验一次，避免排队等待期间又有更新的
        /// Init() 会话开始，导致过期结果仍然被通知给接入方。
        /// </summary>
        private static long generation = 0;

        /// <summary>
        /// TapTapSDK.Init() 同步初始化体（platformWrapper.Init 之后的 IInitTask 循环、
        /// TapTapEvent.Init 等）尚未跑完的会话号；-1 表示当前没有"同步体正在跑"的会话。
        /// Mobile/Standalone 的原生结果是异步到达的，如果在这个窗口内到达，说明它比本地
        /// 同步初始化还快，必须先缓冲，等本地同步初始化真正跑完（成功）后再决定是否发布，
        /// 否则接入方可能先收到"成功"，随后本地同步初始化才失败，状态与实际不一致
        /// （Codex 审查发现）。
        /// </summary>
        private static long pendingSyncSession = -1;

        private static bool hasBufferedNativeResult;
        private static TapInitState bufferedState;
        private static int bufferedErrorCode;
        private static string bufferedErrorMsg;

        /// <summary>
        /// 状态机进入 InProgress，代际号递增，返回本次会话号。调用方（Mobile/Standalone）
        /// 必须把这个会话号一路带到 UpdateSuccess/UpdateFailed，不能自己另外维护一套独立
        /// 的代际号——两套独立计数在时间上并不同步，会导致某一方持有旧会话号通过了自己的
        /// 校验，却在调用 UpdateSuccess/UpdateFailed 时已经是全局意义上的新会话。
        /// </summary>
        public static long SetInProgress()
        {
            lock (Lock)
            {
                generation++;
                state = TapInitState.InProgress;
                return generation;
            }
        }

        /// <summary>
        /// 标记本次会话的 TapTapSDK.Init() 同步初始化体开始执行。必须在 SetInProgress()
        /// 之后、platformWrapper.Init() 之前调用，配合 EndSyncInitSuccess/EndSyncInitFailed
        /// 使用，缓冲同步初始化体跑完之前到达的原生（Mobile/Standalone）终态结果。
        /// </summary>
        public static void BeginSyncInit(long session)
        {
            lock (Lock)
            {
                pendingSyncSession = session;
                hasBufferedNativeResult = false;
            }
        }

        /// <summary>
        /// 本次会话的同步初始化体（IInitTask 循环、TapTapEvent.Init 等）全部跑完且没有
        /// 抛异常。放开缓冲：如果在此之前已经有原生结果到达并被缓冲，现在按正常路径
        /// （重新校验代际号等）发布；如果还没有，之后到达的原生结果会直接走正常路径。
        /// </summary>
        public static void EndSyncInitSuccess(long session)
        {
            bool applySuccess = false;
            bool applyFailed = false;
            int code = 0;
            string msg = null;
            lock (Lock)
            {
                if (pendingSyncSession != session)
                {
                    // 已经有更新的 Init() 会话开始，本次同步初始化完成的通知已经过期
                    return;
                }
                pendingSyncSession = -1;
                if (hasBufferedNativeResult)
                {
                    hasBufferedNativeResult = false;
                    if (bufferedState == TapInitState.Success)
                    {
                        applySuccess = true;
                    }
                    else if (bufferedState == TapInitState.Failed)
                    {
                        applyFailed = true;
                        code = bufferedErrorCode;
                        msg = bufferedErrorMsg;
                    }
                }
            }
            if (applySuccess)
            {
                UpdateSuccess(session);
            }
            else if (applyFailed)
            {
                UpdateFailed(session, code, msg);
            }
        }

        /// <summary>
        /// 本次会话的同步初始化体抛出异常。清除缓冲窗口并丢弃任何已缓冲的原生结果——
        /// 本地同步初始化失败优先于晚到的原生成功结果，然后按正常路径发布这次失败。
        /// </summary>
        public static void EndSyncInitFailed(long session, int failedErrorCode, string failedErrorMsg)
        {
            lock (Lock)
            {
                if (pendingSyncSession == session)
                {
                    pendingSyncSession = -1;
                    hasBufferedNativeResult = false;
                }
            }
            UpdateFailed(session, failedErrorCode, failedErrorMsg);
        }

        /// <summary>
        /// 当前会话号。用于没有机会以参数形式接收会话号的场景（例如 Mobile 桥接的
        /// 原生回调是常驻注册、不属于某一次具体的 Init() 调用），在真正拿到原生结果的
        /// 那一刻读取"当前是哪个会话"，作为 UpdateSuccess/UpdateFailed 的会话号参数。
        /// </summary>
        public static long CurrentSession
        {
            get
            {
                lock (Lock)
                {
                    return generation;
                }
            }
        }

        /// <summary>
        /// 判断给定的会话号是否仍是当前会话，用于调用方在执行副作用（磁盘缓存写入、
        /// 事件通知等）前先做一次快速校验，避免为已经过期的会话做多余的工作。
        /// 最终是否落地状态机仍以 UpdateSuccess/UpdateFailed 内部的校验为准。
        /// </summary>
        public static bool IsSessionCurrent(long session)
        {
            lock (Lock)
            {
                return session == generation;
            }
        }

        /// <summary>
        /// 注册初始化结果回调。若当前已是终态（Success/Failed），会尽快在主线程异步回调一次（并非同步，调用返回后结果不一定已经可用）。
        /// 对同一个 callback 重复调用本方法不会重复加入注册表，但只要当前仍是终态，
        /// 每次调用仍会重新补发一次终态回调；如果不希望重复收到通知，请只在真正需要
        /// 注册时调用一次。
        /// </summary>
        public static void AddCallback(ITapInitCallback callback)
        {
            if (callback == null)
            {
                return;
            }

            TapInitState currentState;
            int currentErrorCode;
            string currentErrorMsg;
            long session;
            lock (Lock)
            {
                if (!callbacks.Contains(callback))
                {
                    callbacks.Add(callback);
                }
                currentState = state;
                currentErrorCode = errorCode;
                currentErrorMsg = errorMsg;
                session = generation;
            }
            DispatchIfTerminal(currentState, currentErrorCode, currentErrorMsg, callback, session);
        }

        /// <summary>
        /// 注销初始化结果回调
        /// </summary>
        public static void RemoveCallback(ITapInitCallback callback)
        {
            if (callback == null)
            {
                return;
            }
            lock (Lock)
            {
                callbacks.Remove(callback);
            }
        }

        /// <summary>
        /// 当前是否已成功初始化（本次会话 gatekeeper 网络请求真正成功）
        /// </summary>
        public static bool IsSuccess()
        {
            lock (Lock)
            {
                return state == TapInitState.Success;
            }
        }

        /// <summary>
        /// 本地同步初始化已经开始：含 InProgress（gatekeeper 还在飞）和 Success。
        /// 用于 SDK 内部读取本地缓存，不要求异步校验完成。
        /// </summary>
        public static bool IsLocalInitStarted()
        {
            lock (Lock)
            {
                return state == TapInitState.InProgress || state == TapInitState.Success;
            }
        }

        /// <summary>
        /// 状态机进入 Success 终态，通知所有已注册回调。
        /// </summary>
        /// <param name="session">产生这次结果的会话号，来自 SetInProgress 的返回值或
        /// CurrentSession。如果调用时全局代际号已经不是这个会话号了，说明这是一次更晚
        /// 启动的新 Init() 会话已经覆盖了它，本次更新会被丢弃。</param>
        public static void UpdateSuccess(long session)
        {
            List<ITapInitCallback> snapshot;
            lock (Lock)
            {
                if (session != generation)
                {
                    return;
                }
                if (session == pendingSyncSession)
                {
                    // 本次会话的同步初始化体（IInitTask 循环等）还没跑完，这个原生结果
                    // 到达得比本地同步初始化还快，先缓冲，等 EndSyncInitSuccess/Failed
                    // 决定是否真正发布，避免接入方先收到成功、随后本地同步初始化才失败
                    hasBufferedNativeResult = true;
                    bufferedState = TapInitState.Success;
                    bufferedErrorCode = 0;
                    bufferedErrorMsg = null;
                    return;
                }
                // 终态只能落地一次：本次会话已经是 Success/Failed 之后，任何后续更新（包括
                // Failed→Success、Success→Failed 以及重复的同一终态）都必须丢弃，否则模块
                // 初始化的同步步骤先报 Failed 后，晚到的 gatekeeper 网络结果仍可能把它覆盖
                // 成 Success，违反"终态只通知一次"的语义（Codex 审查发现）
                if (state == TapInitState.Success || state == TapInitState.Failed)
                {
                    return;
                }
                state = TapInitState.Success;
                errorCode = 0;
                errorMsg = null;
                snapshot = new List<ITapInitCallback>(callbacks);
            }
            TapLoom.QueueOnMainThread(() =>
            {
                lock (Lock)
                {
                    if (session != generation)
                    {
                        // 排队等待主线程执行期间又有更新的 Init() 会话开始，这次通知已经
                        // 过期，丢弃，避免把过期结果误通知给接入方
                        return;
                    }
                }
                foreach (ITapInitCallback callback in snapshot)
                {
                    lock (Lock)
                    {
                        // 排队等待主线程执行期间，接入方可能已经调用 RemoveInitCallback
                        // 注销了这个监听器（例如它持有的界面已经被销毁）；这里不能撤回已经
                        // 排队的通知，但至少可以在真正调用前降低触达一个已注销监听器的概率
                        if (!callbacks.Contains(callback))
                        {
                            continue;
                        }
                    }
                    try
                    {
                        callback.OnInitSuccess();
                    }
                    catch (Exception e)
                    {
                        // 隔离单个监听器抛出的异常，避免它阻止同一批次里其它监听器收到通知
                        TapLog.Error("ITapInitCallback.OnInitSuccess threw", e.Message);
                    }
                }
            });
        }

        /// <summary>
        /// 状态机进入 Failed 终态，通知所有已注册回调。
        /// </summary>
        /// <param name="session">产生这次结果的会话号，语义同 UpdateSuccess 的 session 参数。</param>
        public static void UpdateFailed(long session, int failedErrorCode, string failedErrorMsg)
        {
            List<ITapInitCallback> snapshot;
            lock (Lock)
            {
                if (session != generation)
                {
                    return;
                }
                if (session == pendingSyncSession)
                {
                    // 语义同 UpdateSuccess 里的缓冲分支：本地同步初始化体还没跑完，先缓冲
                    hasBufferedNativeResult = true;
                    bufferedState = TapInitState.Failed;
                    bufferedErrorCode = failedErrorCode;
                    bufferedErrorMsg = failedErrorMsg;
                    return;
                }
                // 语义同 UpdateSuccess：终态只能落地一次，同一会话不允许 Success→Failed
                if (state == TapInitState.Success || state == TapInitState.Failed)
                {
                    return;
                }
                state = TapInitState.Failed;
                errorCode = failedErrorCode;
                errorMsg = failedErrorMsg;
                snapshot = new List<ITapInitCallback>(callbacks);
            }
            TapLoom.QueueOnMainThread(() =>
            {
                lock (Lock)
                {
                    if (session != generation)
                    {
                        // 排队等待主线程执行期间又有更新的 Init() 会话开始，这次通知已经
                        // 过期，丢弃，避免把过期结果误通知给接入方
                        return;
                    }
                }
                foreach (ITapInitCallback callback in snapshot)
                {
                    lock (Lock)
                    {
                        if (!callbacks.Contains(callback))
                        {
                            continue;
                        }
                    }
                    try
                    {
                        callback.OnInitFail(failedErrorCode, failedErrorMsg);
                    }
                    catch (Exception e)
                    {
                        // 隔离单个监听器抛出的异常，避免它阻止同一批次里其它监听器收到通知
                        TapLog.Error("ITapInitCallback.OnInitFail threw", e.Message);
                    }
                }
            });
        }

        /// <summary>
        /// 供 CheckInitState 一类的同步检查读取：未初始化/进行中/失败时返回对应的提示文案
        /// </summary>
        /// <summary>
        /// 本地缓存是否还不可用。InProgress 视为可用（不返回文案）。
        /// Idle / Failed 才需要提示。
        /// </summary>
        public static bool TryGetLocalUnavailableMessage(out string shortMsg, out string detailMsg)
        {
            TapInitState currentState;
            string currentErrorMsg;
            lock (Lock)
            {
                currentState = state;
                currentErrorMsg = errorMsg;
            }
            if (currentState == TapInitState.Success || currentState == TapInitState.InProgress)
            {
                shortMsg = null;
                detailMsg = null;
                return false;
            }
            if (currentState == TapInitState.Failed && !string.IsNullOrEmpty(currentErrorMsg))
            {
                shortMsg = currentErrorMsg;
                detailMsg = currentErrorMsg;
            }
            else
            {
                shortMsg = "当前应用还未初始化";
                detailMsg = "当前应用还未初始化: 请在调用 SDK 业务接口前，先调用 TapTapSDK.Init  接口";
            }
            return true;
        }

        public static bool TryGetNonSuccessMessage(out string shortMsg, out string detailMsg)
        {
            TapInitState currentState;
            string currentErrorMsg;
            lock (Lock)
            {
                currentState = state;
                currentErrorMsg = errorMsg;
            }
            if (currentState == TapInitState.Success)
            {
                shortMsg = null;
                detailMsg = null;
                return false;
            }
            if (currentState == TapInitState.Failed && !string.IsNullOrEmpty(currentErrorMsg))
            {
                shortMsg = currentErrorMsg;
                detailMsg = currentErrorMsg;
            }
            else if (currentState == TapInitState.InProgress)
            {
                // Init() 已经调用过，只是 gatekeeper 网络请求还没返回结果，不能和
                // "从未调用 Init()"用同一句话，否则接入方会误以为自己漏调了初始化
                // （用户反馈：断网时看到这句提示，误以为是没调用 init 导致的）
                shortMsg = "SDK 正在初始化中，请稍后再试";
                detailMsg = "SDK 正在初始化中，请稍后再试";
            }
            else
            {
                shortMsg = "当前应用还未初始化";
                detailMsg = "当前应用还未初始化: 请在调用 SDK 业务接口前，先调用 TapTapSDK.Init  接口";
            }
            return true;
        }

        private static void DispatchIfTerminal(TapInitState currentState, int currentErrorCode, string currentErrorMsg, ITapInitCallback callback, long session)
        {
            if (currentState == TapInitState.Success)
            {
                TapLoom.QueueOnMainThread(() =>
                {
                    lock (Lock)
                    {
                        if (session != generation || !callbacks.Contains(callback))
                        {
                            return;
                        }
                    }
                    try
                    {
                        callback.OnInitSuccess();
                    }
                    catch (Exception e)
                    {
                        TapLog.Error("ITapInitCallback.OnInitSuccess threw", e.Message);
                    }
                });
            }
            else if (currentState == TapInitState.Failed)
            {
                TapLoom.QueueOnMainThread(() =>
                {
                    lock (Lock)
                    {
                        if (session != generation || !callbacks.Contains(callback))
                        {
                            return;
                        }
                    }
                    try
                    {
                        callback.OnInitFail(currentErrorCode, currentErrorMsg);
                    }
                    catch (Exception e)
                    {
                        TapLog.Error("ITapInitCallback.OnInitFail threw", e.Message);
                    }
                });
            }
        }
    }
}
