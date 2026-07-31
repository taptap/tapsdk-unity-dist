using System;
using System.Collections;
using System.Threading.Tasks;
using TapSDK.Compliance.Internal;
using TapSDK.Core;
using TapSDK.Core.Internal.Log;
using TapSDK.Login;
using UnityEngine;
using Network = TapSDK.Compliance.Internal.Network;

namespace TapSDK.Compliance 
{
    /// <summary>
    /// 防沉迷轮询器
    /// </summary>
    internal class CompliancePoll : MonoBehaviour 
    {
        static readonly string ANTI_ADDICTION_POLL_NAME = "CompliancePoll";

        static CompliancePoll current;

        /// <summary>
        /// 轮询间隔，单位：秒
        /// </summary>
        private static int pollInterval = 2 * 60;

        private static Coroutine _pollCoroutine;

        private static float? _elpased;

        public static bool StartPoll;

        /// <summary>
        /// 轮询生命周期的代次。StartUp / Logout 都是<b>跨线程</b>的独立公开入口：
        /// StartUp 常挂在 HTTP 响应续体上（OnCheckedPlayableWithAdult → TryStartPoll，
        /// 成年账号也走），跑在线程池、经闸门<b>入队</b>；而 Logout 可以从主线程直接进来
        /// （OnApplicationPause → LeaveGame，以及游戏调 exit），在主线程会<b>同步执行</b>。
        ///
        /// 于是存在这样一条真实序列：后台 StartUp 入队 → 主线程 Logout 立刻把
        /// _pollCoroutine 置空并返回 → 下一帧队列里那个过期的启动动作照旧创建协程、
        /// 把 StartPoll 重新置 true，结果<b>用户已退出、合规心跳还在发</b>。
        ///
        /// 每次 StartUp / Logout 都递增代次，入队的启动动作执行前先比对自己捕获的代次，
        /// 不一致就说明期间已有更新的意图，直接作废。
        ///
        /// 代次和 StartPoll / _elpased 必须在同一个临界区里改：只用 Interlocked 递增代次的
        /// 话，"递增" 和 "写 StartPoll" 之间仍有窗口——StartUp 递增到 1 后被 Logout 抢占
        /// （代次到 2、StartPoll 置 false、协程同步停掉），StartUp 再把 StartPoll 写回 true，
        /// 而它的启动动作稍后因代次失效被跳过，于是留下"没有协程在跑、StartPoll 却是 true"
        /// 的脏状态：LeaveGame 会据此设 needResumePoll，EnterGame 再把轮询错误地拉起来。
        /// </summary>
        private static int pollGeneration;

        /// <summary>
        /// 保护 pollGeneration / StartPoll / _elpased / _pollCoroutine 的一致性。
        /// 临界区里只有几个赋值和协程启停，不做 IO；Monitor 可重入，协程首段若回调进来也
        /// 不会自锁。
        /// </summary>
        private static readonly object pollStateLock = new object();

        internal static void StartUp(int inverval = 0) 
        {
            TapLog.Log("StartUp " );
            if(inverval > 0){
                pollInterval = inverval;
            }
            // new GameObject / DontDestroyOnLoad / AddComponent / StartCoroutine 全是只能在
            // 主线程调的 Unity API，而这里的调用方 TryStartPoll 挂在 HTTP 响应的续体上——
            // 成年账号（OnCheckedPlayableWithAdult → TryStartPoll）也走这条路，不只是弹面板
            // 的未成年分支。之前没炸只是因为 current 早就在主线程建好了、走不到这个 if。
            int generation;
            // 代次递增和 StartPoll 置位必须原子：中间若被 Logout 抢占，这里的 StartPoll = true
            // 会覆盖它的 false，而启动动作又会因代次失效被跳过，留下"无协程但 StartPoll=true"
            // 的脏状态（详见 pollGeneration 的说明）。
            //
            // StartPoll 也不能等派发执行才置位：LeaveGame 用它判断"是否需要停"
            // （if (CanPlay && CompliancePoll.StartPoll)），若等到下一帧，切后台时 LeaveGame
            // 会看到 false 而跳过停止、也不设 needResumePoll，随后队列里的启动动作照旧把轮询
            // 拉起来 —— 后台里继续发心跳，且回到前台后再也不会被恢复。
            lock (pollStateLock)
            {
                generation = ++pollGeneration;
                StartPoll = true;
            }
            ComplianceMainThread.Run(() =>
            {
                lock (pollStateLock)
                {
                    // 期间发生过 Logout（或更晚的一次 StartUp），本次启动意图已过期
                    if (generation != pollGeneration)
                    {
                        return;
                    }
                    if (current == null)
                    {
                        GameObject pollGo = new GameObject(ANTI_ADDICTION_POLL_NAME);
                        DontDestroyOnLoad(pollGo);
                        current = pollGo.AddComponent<CompliancePoll>();
                        _elpased = null;
                    }

                    if (_pollCoroutine == null)
                    {
                        _pollCoroutine = current.StartCoroutine(current.Poll());
                        StartPoll = true;
                    }
                }
            });
        }
        
        internal static void StartCountdownRemainTime() 
        {
            TapLog.Log("StartCountdownRemainTime  " );
            // 同 StartUp：建 GameObject 只能在主线程。日志里这个方法确实出现在 IO 线程上
            // （"(IO 31) StartCountdownRemainTime"），只是当时 current 已存在、提前 return
            // 才没事——首次从后台线程进来就会踩中。_elpased 由 Update 读，一起放进闸门。
            //
            // 这里只<b>校验</b>代次、不递增：它与 StartUp 不互斥（一个是本地倒计时、一个是
            // 服务端轮询），递增会误伤在途的 StartUp。校验的作用是防止 Logout 已经把
            // _elpased 清空后，队列里这个过期动作又把倒计时重新点起来。
            int generation;
            lock (pollStateLock)
            {
                generation = pollGeneration;
            }
            ComplianceMainThread.Run(() =>
            {
                lock (pollStateLock)
                {
                    if (generation != pollGeneration)
                    {
                        return;
                    }
                    if (current == null)
                    {
                        GameObject pollGo = new GameObject(ANTI_ADDICTION_POLL_NAME);
                        DontDestroyOnLoad(pollGo);
                        current = pollGo.AddComponent<CompliancePoll>();
                        _elpased = null;
                    }
                    else
                    {
                        return;
                    }

                    _elpased = 0;
                }
            });
        }

        internal static void Logout()
        {
            // 先递增代次，作废所有仍在主线程队列里等待执行的 StartUp / 倒计时启动动作，
            // 否则退出后它们会把轮询重新拉起来（详见 pollGeneration 的说明）。
            int generation;
            // 同 StartUp：代次递增与状态写入原子完成，避免与并发的 StartUp 互相覆盖。
            // StartPoll / _elpased 留在当前线程立即生效——调用方（exit / 登出）依赖
            // "调用返回后轮询判定立刻失效"，延后一帧会让 Update 多跑一次心跳。
            lock (pollStateLock)
            {
                generation = ++pollGeneration;
                StartPoll = false;
                _elpased = null;
            }
            // StopAllCoroutines 是 Unity API，只能主线程；_pollCoroutine 置空必须跟它同批，
            // 否则 StartUp 可能在协程真正停掉之前就重启一个。
            //
            // 停止动作同样要校验代次，方向与 StartUp 相反但成因一样：后台线程 Logout 把
            // 停止动作入队后，主线程可能紧接着 StartUp（同步）启动了新一轮轮询，此时这个
            // 过期的停止动作若照旧执行，就会掐掉刚建立的新会话并把 _pollCoroutine 置空
            // ——StartPoll 还是 true，状态显示在轮询、实际心跳已停，比直接报错更难查。
            ComplianceMainThread.Run(() =>
            {
                lock (pollStateLock)
                {
                    if (generation != pollGeneration)
                    {
                        return;
                    }
                    current?.StopAllCoroutines();
                    _pollCoroutine = null;
                }
            });
        }

        private void Update()
        {
            if (_elpased != null)
            {
                _elpased += Time.unscaledDeltaTime;
                if (_elpased >= 1)
                {
                    _elpased = 0;
                    if (TapTapComplianceManager.CurrentRemainSeconds != null)
                        TapTapComplianceManager.CurrentRemainSeconds--;
                }
            }
        }

        IEnumerator Poll()
        {
            // 记下自己属于哪一代。协程退出时只有在代次未变（自己仍是当前会话）的情况下才复位
            // 共享状态，否则会误清掉一个更新的 StartUp 刚建立的轮询。
            int generation;
            lock (pollStateLock)
            {
                generation = pollGeneration;
            }
            // 用 try/finally 兜住所有退出路径（正常 break、Result 抛异常、被 StopAllCoroutines
            // 中断）。原来没有这层兜底：checkPlayableTask 一旦 faulted 或 canceled，下面读
            // .Result 会抛 AggregateException 把协程掀掉，而 _pollCoroutine 仍非 null、
            // StartPoll 仍是 true —— 之后 StartUp() 因为 _pollCoroutine != null 不再启动，
            // 轮询就永久停了，状态却一直显示在跑。
            try
            {
                while (true)
                {
                    // 上报/检查可玩
                    Task<PlayableResult> checkPlayableTask = TapTapComplianceManager.CheckPlayableOnPolling();
                    yield return new WaitUntil(() => checkPlayableTask.IsCompleted);
                    // 先判失败再读 Result，别让异常来终止协程
                    if (checkPlayableTask.IsFaulted || checkPlayableTask.IsCanceled)
                    {
                        TapLog.Error("[Compliance] 轮询检查可玩性失败，停止轮询: "
                            + (checkPlayableTask.Exception?.GetBaseException().Message ?? "canceled"));
                        break;
                    }
                    PlayableResult playable = checkPlayableTask.Result;
                    if (playable.RemainTime <= 0)
                    {
                        break;
                    }
                    if(playable.RemainTime > 0 && playable.RemainTime < pollInterval){
                        pollInterval = playable.RemainTime;
                    }
                    if (_elpased == null)
                        _elpased = 0;

                    yield return new WaitForSecondsRealtime(pollInterval);
                }
            }
            finally
            {
                lock (pollStateLock)
                {
                    if (generation == pollGeneration)
                    {
                        _elpased = null;
                        _pollCoroutine = null;
                        StartPoll = false;
                    }
                }
            }
        }

        /// <summary>
        /// 切换后台
        /// </summary>
        /// <param name="pauseStatus"></param>
        void OnApplicationPause(bool pauseStatus)
        {
            TapLog.Log("Anti OnApplicationPause " + pauseStatus);
           if(pauseStatus){
                TapTapComplianceManager.LeaveGame();
           }else{
                TapTapComplianceManager.EnterGame();
           } 
        }


        private static void SendPlayableRequest()
        {
#pragma warning disable CS4014
            Network.CheckPlayable();
#pragma warning restore CS4014
        }
    }
}
