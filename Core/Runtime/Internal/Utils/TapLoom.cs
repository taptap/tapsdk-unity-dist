using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TapSDK.Core.Internal.Utils
{
    public class TapLoom : MonoBehaviour
    {
        public static int maxThreads = 8;
        static int numThreads;

        private static TapLoom _current;
        private int _count;

        private bool isPause = false;

        // 记录主线程 ID
        private static int _mainThreadId = -1;
        private static readonly object _pendingActionsLock = new object();
        private static readonly List<Action> _pendingActions = new List<Action>();
#if UNITY_EDITOR
        private static bool editorPlayModeStateChangedRegistered;
        private static bool editorPauseStateChangedRegistered;
        private static bool applicationQuitTriggered;
#endif

        public static TapLoom Current
        {
            get
            {
                Initialize();
                return _current;
            }
        }

        /// <summary>
        /// Awake() 是否已经跑过、记录到了权威的主线程 ID。调用方在这个属性返回 false
        /// 时无法判断"当前线程是不是主线程"，不应该据此拒绝调用（Codex 审查发现：
        /// 不能把"哪个线程第一次触发了某个类型的静态构造器"当成主线程的代理——一旦
        /// 后台线程提前访问到那个类型，会永久把错误的线程当成主线程，反而让后续真正
        /// 从主线程发起的合法调用被拒绝）。
        /// </summary>
        public static bool IsMainThreadKnown => _mainThreadId >= 0;

        /// <summary>
        /// 当前线程是否是 Awake() 记录到的主线程；IsMainThreadKnown 为 false 时始终
        /// 返回 true（还没有可信基准，不能拒绝）。Awake() 由 Unity 引擎保证只会在
        /// 主线程被调用，是比自行猜测更权威的主线程标识。
        /// </summary>
        public static bool IsMainThread =>
            _mainThreadId < 0 || Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>
        /// 在 Unity 运行时加载阶段（保证在主线程执行，早于任何场景的 Awake/Start，甚至
        /// 早于用户脚本代码）就主动记录主线程 ID，不依赖 SDK 代码先调用 Initialize()
        /// 才被动记录。之前只在 TapLoom 的 MonoBehaviour Awake() 里记录，而 Awake()
        /// 只有在某个 SDK 模块自己调用了 Initialize() 创建 GameObject 之后才会触发——
        /// 如果调用方在那之前就从背景线程调用了 TapTapSDK.Init()，IsMainThreadKnown
        /// 会是 false，IsMainThread 默认放行，反而漏掉了这次误用（Codex 审查发现）。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void CaptureMainThreadOnLoad()
        {
            if (_mainThreadId < 0)
            {
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            }
        }

        void Awake()
        {
            _current = this;
            initialized = true;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            lock (_pendingActionsLock)
            {
                lock (_actions)
                {
                    _actions.AddRange(_pendingActions);
                    _pendingActions.Clear();
                }
            }
#if UNITY_EDITOR
            BindEditorLifecycleEvents();
#endif
        }

        static bool initialized;

        public static void Initialize()
        {
#if UNITY_EDITOR
            if (IsApplicationPlaying())
            {
                BindEditorLifecycleEvents();
            }
#endif
            if (!initialized)
            {
                if (!IsApplicationPlaying())
                    return;
                var g = new GameObject("TapLoom");
                DontDestroyOnLoad(g);
                _current = g.AddComponent<TapLoom>();
            }
        }

        private static bool IsApplicationPlaying()
        {
            try
            {
                return Application.isPlaying;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private List<Action> _actions = new List<Action>();

        public struct DelayedQueueItem
        {
            public float time;
            public Action action;
        }

        private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

        List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

        public static void QueueOnMainThread(Action action)
        {
            QueueOnMainThread(action, 0f);
        }

        public static void QueueOnMainThread(Action action, float time)
        {
            if (action == null)
            {
                return;
            }
            if (!IsMainThread && !initialized)
            {
                lock (_pendingActionsLock)
                {
                    _pendingActions.Add(action);
                }
                return;
            }
            if (time != 0)
            {
                lock (Current._delayed)
                {
                    Current._delayed.Add(
                        new DelayedQueueItem { time = Time.time, action = action }
                    );
                }
            }
            else
            {
                if (Current != null && Current._actions != null)
                {
                    lock (Current._actions)
                    {
                        Current._actions.Add(action);
                    }
                }
            }
        }

        /// <summary>
        /// 在线程池中执行任务，非主线程
        /// </summary>
        /// <param name="a"> 任务 </param>
        /// <returns></returns>
        public static Thread RunAsync(Action a)
        {
            Initialize();
            while (numThreads >= maxThreads)
            {
                Thread.Sleep(1);
            }
            Interlocked.Increment(ref numThreads);
            ThreadPool.QueueUserWorkItem(RunAction, a);
            return null;
        }

        /// <summary>
        /// 阻塞式在主线程执行任务并返回值，当发生异常或超时时，返回默认值
        /// </summary>
        /// <param name="func"> 任务 </param>
        /// <param name="defaultValue"> 默认值 </param>
        /// <param name="timeout"> 超时时间，默认 100 毫秒</param>
        /// <returns> 任务返回值或默认值 </returns>
        public static object RunOnMainThreadSync(
            Func<object> func,
            object defaultValue,
            int timeout = 100
        )
        {
            // 主线程未就绪,直接返回默认值
            if (_mainThreadId < 0)
            {
                return defaultValue;
            }
            // 已经在主线程，直接执行
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                return func();
            }
            object result = defaultValue;
            var evt = new ManualResetEvent(false);
            try
            {
                QueueOnMainThread(() =>
                {
                    try
                    {
                        result = func();
                    }
                    catch (Exception ex)
                    {
                        TapLogger.Error("RunOnMainThreadSync failed " + ex.Message);
                    }
                    finally
                    {
                        try
                        {
                            evt.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                            // evt 已被释放，直接忽略
                        }
                    }
                });

                evt.WaitOne(timeout);
            }
            finally
            {
                evt.Dispose(); // WaitOne 返回后再 Dispose
            }
            return result;
        }

        private static void RunAction(object action)
        {
            try
            {
                ((Action)action)();
            }
            catch { }
            finally
            {
                Interlocked.Decrement(ref numThreads);
            }
        }

        void OnDisable()
        {
            if (_current == this)
            {
                _current = null;
            }
        }

#if UNITY_EDITOR
        private static void BindEditorLifecycleEvents()
        {
            BindEditorPlayModeStateChanged();
            BindEditorPauseStateChanged();
        }

        private static void BindEditorPlayModeStateChanged()
        {
            if (editorPlayModeStateChangedRegistered)
            {
                return;
            }
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            editorPlayModeStateChangedRegistered = true;
        }

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                TriggerApplicationQuit();
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                applicationQuitTriggered = false;
                EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
                editorPlayModeStateChangedRegistered = false;
                EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
                editorPauseStateChangedRegistered = false;
            }
        }

        private static void BindEditorPauseStateChanged()
        {
            if (editorPauseStateChangedRegistered)
            {
                return;
            }
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
            editorPauseStateChangedRegistered = true;
        }

        private static void OnEditorPauseStateChanged(PauseState state)
        {
            EventManager.TriggerEvent(
                EventManager.OnApplicationPause,
                state == PauseState.Paused
            );
        }

#endif

        // Use this for initialization
        void Start() { }

        List<Action> _currentActions = new List<Action>();

        // Update is called once per frame
        void Update()
        {
            lock (_actions)
            {
                _currentActions.Clear();
                _currentActions.AddRange(_actions);
                _actions.Clear();
            }
            foreach (var a in _currentActions)
            {
                a();
            }
            lock (_delayed)
            {
                _currentDelayed.Clear();
                _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));
                foreach (var item in _currentDelayed)
                    _delayed.Remove(item);
            }
            foreach (var delayed in _currentDelayed)
            {
                delayed.action();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && isPause == false)
            {
                isPause = true;
                EventManager.TriggerEvent(EventManager.OnApplicationPause, true);
            }
            else if (!pauseStatus && isPause)
            {
                isPause = false;
                EventManager.TriggerEvent(EventManager.OnApplicationPause, false);
            }
        }

        private void OnApplicationQuit()
        {
            TriggerApplicationQuit();
        }

        private static void TriggerApplicationQuit()
        {
#if UNITY_EDITOR
            if (applicationQuitTriggered)
            {
                return;
            }
            applicationQuitTriggered = true;
#endif
            EventManager.TriggerEvent(EventManager.OnApplicationQuit, true);
        }
    }
}
