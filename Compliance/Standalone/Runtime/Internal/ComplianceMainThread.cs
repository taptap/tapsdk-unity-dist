using System;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Internal.Utils;

namespace TapSDK.Compliance.Internal
{
    /// <summary>
    /// 合规模块调用 Unity API 的主线程闸门（UI、协程、PlayerPrefs 都走这里）。
    ///
    /// 合规的这些调用方几乎全挂在 HTTP 响应的续体上（CheckPlayable / heartbeat / 实名认证
    /// 之后）。只要 Startup 是从后台线程发起的，那条线程上没有 UnitySynchronizationContext
    /// 可捕获，整条 async 链的续体就都留在线程池上，此时任何 Unity API 调用都会出事：
    ///   - Resources.Load / UIManager.OpenUI → "Graphics device is null." 原生崩溃，拦不住；
    ///   - UIManager.Instance 首次访问 → getter 里的 Application.isPlaying / new GameObject
    ///     会抛 UnityException（_instance 已存在时因短路求值不会触发，所以这条是概率性的，
    ///     更难查）；
    ///   - CompliancePoll 的 new GameObject / StartCoroutine / StopAllCoroutines；
    ///   - DataStorage 底层的 PlayerPrefs。
    /// 因此必须把"UIManager.Instance.Xxx(...)"这类完整表达式一起送进主线程，只包住方法体
    /// 内部是不够的——Instance 的访问本身就在闸门之外。
    ///
    /// 主线程调用保持原样同步执行，既有时序一点不变；只有后台线程才改成派发。
    ///
    /// 关于成对调用（OpenLoading/CloseLoading、StartUp/Logout）的顺序：TapLoom 的队列是
    /// FIFO，所以同一线程性质下多次派发保序。跨线程混合（一端同步执行、另一端排队）理论上
    /// 会乱序，但实际不会发生：后台线程发起时整条 async 链没有 SynchronizationContext，
    /// 续体不会自己跑回主线程，两端都会入队；主线程发起时两端都同步执行。
    /// </summary>
    internal static class ComplianceMainThread
    {
        internal static void Run(Action action)
        {
            Run(action, null);
        }

        /// <summary>
        /// onError 用来把派发过程中的异常回传给等待方（通常是 TaskCompletionSource）。
        ///
        /// 派发出去的动作<b>必须</b>捕获异常，原因有两个：
        /// 一是 TapLoom.Update 里那个 foreach 没有 try/catch，任何一个动作抛异常都会让
        /// 本帧队列中其余动作全部不执行——那些动作来自各个模块，互相不该有牵连；
        /// 二是原本同步执行时，异常会冒泡给调用方的 try/catch，改成派发后它只会从
        /// Update 里冒出来，调用方再也接不到，于是 await 那个 Task 的一方<b>永久挂起</b>
        /// （合规的 Startup 流程就会卡死不返回）。
        ///
        /// 没有传 onError 的 fire-and-forget 场景（Toast / Loading 之类），异常记日志
        /// 即可：弹不出一个提示不该被上抛成"认证失败"，更不该拖垮整帧队列。
        /// </summary>
        internal static void Run(Action action, Action<Exception> onError)
        {
            if (action == null)
            {
                return;
            }
            Action guarded = () =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    if (onError != null)
                    {
                        onError(e);
                    }
                    else
                    {
                        TapLog.Error("[Compliance] main-thread action failed: " + e);
                    }
                }
            };
            if (TapLoom.IsMainThread)
            {
                guarded();
                return;
            }
            TapLoom.QueueOnMainThread(guarded);
        }
    }
}
