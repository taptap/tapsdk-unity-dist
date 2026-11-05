
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using TapSDK.Core.Internal.Utils;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone.Internal.Http;
using UnityEngine;
using UnityEngine.Networking;

namespace TapSDK.Core.Standalone.Internal
{
    public class EventSender
    {
        private const string OldEventFilePath = "events.json";

        private readonly TapLog log = new TapLog("TapEvent");
        private string persistentDataPath = TapEventStorage.DataPath ?? Application.persistentDataPath;

        private ConcurrentQueue<Dictionary<string, object>> eventQueue = new ConcurrentQueue<Dictionary<string, object>>();
        private TapHttp tapHttp = TapHttp
            .NewBuilder("TapSDKCore", TapTapSDK.Version)
            .Sign(TapHttpSign.CreateNoneSign())
            .Parser(TapHttpParser.CreateEventParser())
            .Build();

        private const int MaxEvents = 50;
        private const int MaxBatchSize = 200;
        private const float SendInterval = 15f;
        private Timer timer;
        private DateTime lastSendTime;

        // 串行化发送：0=空闲，1=发送中；防止并发批次导致 in-flight 追踪混乱
        private int _isSending = 0;
        // 当前正在发送的批次；SaveEvents 落盘时需合并，避免进程崩溃丢失 in-flight 事件
        private volatile List<Dictionary<string, object>> _inFlightBatch = null;

        private string domain = Constants.SERVER_URL_CN;

        private int QueueCount => eventQueue.Count;
        private readonly object queueGate = new object();
        private Task saving;
        private bool saveRequested;
        private readonly SharedCacheStorage storage;
        private readonly bool isolated = TapEventStorage.IsIsolated;
        private Task loading;
        private bool cacheLoaded;
        private const int MaxPendingEvents = 10000;

        private Task EnsureLoadedAsync()
        {
            lock (queueGate)
            {
                if (loading == null || loading.IsFaulted || loading.IsCanceled) loading = LoadEventsAsync();
                return loading;
            }
        }

        public EventSender()
        {
            lastSendTime = DateTime.Now;

            // 初始化 HttpClient
            var header = new Dictionary<string, string>
            {
                { "User-Agent", $"{TapTapSDK.SDKPlatform}/{TapTapSDK.Version}" }
            };

            var coreOptions = TapCoreStandalone.coreOptions;
            if (coreOptions.region == TapTapRegionType.CN)
            {
                domain = Constants.SERVER_URL_CN;
            }
            else
            {
                domain = Constants.SERVER_URL_IO;
            }

            storage = new SharedCacheStorage(Path.Combine(persistentDataPath, OldEventFilePath),
                Path.Combine(persistentDataPath, GetEventCacheFileName()), false);
            timer = new Timer(OnTimerElapsed, null, TimeSpan.FromSeconds(SendInterval), TimeSpan.FromSeconds(SendInterval));
            SendEventsAsync(null);
        }

        // 历史数据独立分批发送，不进入当前队列、不创建定时器；由调用方等待每批结果。
        internal static async Task<bool> SendHistoryAsync(List<Dictionary<string, object>> events, string region)
        {
            var http = TapHttp.NewBuilder("TapSDKCore", TapTapSDK.Version)
                .Sign(TapHttpSign.CreateNoneSign()).Parser(TapHttpParser.CreateEventParser()).Build();
            string endpoint = region == TapTapRegionType.CN.ToString() ? Constants.SERVER_URL_CN : Constants.SERVER_URL_IO;
            for (int offset = 0; offset < events.Count; offset += MaxBatchSize)
            {
                var body = new Dictionary<string, object> {
                    { "data", events.GetRange(offset, Math.Min(MaxBatchSize, events.Count - offset)) }
                };
                var response = await http.PostJsonAsync<Boolean>(path: $"{endpoint}/v2/batch", json: body);
                if (!response.IsSuccess) return false;
            }
            return true;
        }

        public async void SendEventsAsync(Action onSendComplete)
        {
            try { await EnsureLoadedAsync(); }
            catch (Exception e)
            {
                log.Warning("Event cache not ready; preserving disk and pending events: " + e.Message);
                onSendComplete?.Invoke();
                return;
            }
            if (eventQueue.IsEmpty)
            {
                // A successful upload may have emptied memory while its disk cleanup failed.
                bool retrySave;
                lock (queueGate) retrySave = saveRequested || saving != null;
                if (retrySave) await SaveEventsAsync();
                onSendComplete?.Invoke();
                return;
            }

            // 已有批次在发送中，跳过本次（事件留在队列，下次 tick 继续）
            if (Interlocked.CompareExchange(ref _isSending, 1, 0) != 0)
            {
                onSendComplete?.Invoke();
                return;
            }

            var eventsToSend = new List<Dictionary<string, object>>();
            lock (queueGate)
            {
                while (eventsToSend.Count < MaxBatchSize && eventQueue.TryDequeue(out var item))
                    eventsToSend.Add(item);
                _inFlightBatch = eventsToSend;
            }

            // 并发 TryDequeue 可能导致本次实际取到 0 条
            if (eventsToSend.Count == 0)
            {
                Interlocked.Exchange(ref _isSending, 0);
                onSendComplete?.Invoke();
                return;
            }

            try
            {
                var body = new Dictionary<string, object> {
                    { "data", eventsToSend }
                };

                var response = await tapHttp.PostJsonAsync<Boolean>(path: $"{domain}/v2/batch", json: body);
                if (!response.IsSuccess)
                {
                    log.Warning("Failed to send events");
                    lock (queueGate)
                    {
                        foreach (var eventParams in eventsToSend) eventQueue.Enqueue(eventParams);
                        _inFlightBatch = null;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warning($"SendEventsAsync exception: {ex.ToString()}");
                lock (queueGate)
                {
                    foreach (var eventParams in eventsToSend) eventQueue.Enqueue(eventParams);
                    _inFlightBatch = null;
                }
            }
            finally
            {
                // 先清 in-flight 再落盘：此时事件已回队（失败）或确认完成（成功）
                lock (queueGate) { _inFlightBatch = null; }
                await SaveEventsAsync();
                Interlocked.Exchange(ref _isSending, 0);
                // 回调放在持久化之后，隔离外部异常不影响落盘
                try { onSendComplete?.Invoke(); } catch (Exception ex) { log.Warning($"onSendComplete exception: {ex.Message}"); }
            }
        }

        public void Send(Dictionary<string, object> eventParams)
        {
            // 将事件添加到队列
            lock (queueGate)
            {
                if (!cacheLoaded && eventQueue.Count >= MaxPendingEvents)
                {
                    log.Warning("Event cache unavailable; pending buffer full, rejecting new event.");
                    return;
                }
                eventQueue.Enqueue(eventParams);
            }
            _ = SaveEventsAsync();

            // 检查队列大小
            if (QueueCount >= MaxEvents)
            {
                SendEvents();
                ResetTimer();
            }
        }

        private void OnTimerElapsed(object state)
        {
            var offset = (DateTime.Now - lastSendTime).TotalSeconds;
            if (offset >= SendInterval)
            {
                SendEvents();
                ResetTimer();
            }
        }


        private void ResetTimer()
        {
            timer.Change(TimeSpan.FromSeconds(SendInterval), TimeSpan.FromSeconds(SendInterval));
        }

        private string GetEventCacheFileName(){
            if (TapEventStorage.IsIsolated) return TapEventStorage.EventsFile;
            if (TapTapSDK.taptapSdkOptions != null 
            && !string.IsNullOrEmpty(TapTapSDK.taptapSdkOptions.clientId)){
                return "events_" + TapTapSDK.taptapSdkOptions.clientId + ".json";
            }
            return OldEventFilePath;
        }

        private async Task LoadEventsAsync()
        {
            await storage.PrepareAsync().ConfigureAwait(false);
            using (isolated ? null : await SharedCacheFile.AcquireAsync(storage.Target).ConfigureAwait(false))
            {
                var savedEvents = new List<Dictionary<string, object>>();
                if (File.Exists(storage.Target))
                {
                    string jsonData = File.ReadAllText(storage.Target);
                    savedEvents = ConvertToListOfDictionaries(Json.Deserialize(jsonData));
                    if (savedEvents == null) throw new InvalidDataException("Event cache is invalid; original data retained.");
                }
                lock (queueGate)
                {
                    // Recover older events before events accumulated while storage was unavailable.
                    savedEvents.AddRange(eventQueue.ToArray());
                    eventQueue = new ConcurrentQueue<Dictionary<string, object>>(savedEvents);
                    cacheLoaded = true;
                }
            }
        }

        private Task SaveEventsAsync()
        {
            TaskCompletionSource<bool> completion;
            lock (queueGate)
            {
                saveRequested = true;
                if (saving != null) return saving;
                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                saving = completion.Task;
            }
            // Run inline until IO must wait, preserving normal single-process immediate persistence.
            _ = PersistEventsAsync(completion);
            return completion.Task;
        }

        private async Task PersistEventsAsync(TaskCompletionSource<bool> completion)
        {
            try
            {
                await EnsureLoadedAsync().ConfigureAwait(false);
                while (true)
                {
                    using (isolated ? null : await SharedCacheFile.AcquireAsync(storage.Target).ConfigureAwait(false))
                    {
                        List<Dictionary<string, object>> eventList;
                        lock (queueGate)
                        {
                            eventList = new List<Dictionary<string, object>>(eventQueue.ToArray());
                            if (_inFlightBatch != null) eventList.AddRange(_inFlightBatch);
                            saveRequested = false;
                        }
                        SharedCacheFile.WriteTextUnderLock(storage.Target, Json.Serialize(eventList));
                    }
                    lock (queueGate)
                    {
                        if (saveRequested) continue;
                        saving = null;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                lock (queueGate)
                {
                    saveRequested = true;
                    saving = null;
                }
                log.Warning("Event cache save deferred; original data retained: " + e.Message);
            }
            finally { completion.TrySetResult(true); }
        }

        public void SendEvents()
        {
            SendEventsAsync(() => lastSendTime = DateTime.Now);
        }

        private Dictionary<string, object> ConvertToDictionary(Dictionary<string, object> original)
        {
            var result = new Dictionary<string, object>();
            foreach (var keyValuePair in original)
            {
                if (keyValuePair.Value is Dictionary<string, object> nestedDictionary)
                {
                    result[keyValuePair.Key] = ConvertToDictionary(nestedDictionary);
                }
                else if (keyValuePair.Value is List<object> nestedList)
                {
                    result[keyValuePair.Key] = ConvertToListOfDictionaries(nestedList);
                }
                else
                {
                    result[keyValuePair.Key] = keyValuePair.Value;
                }
            }
            return result;
        }
        private List<Dictionary<string, object>> ConvertToListOfDictionaries(object deserializedData)
        {
            if (deserializedData is List<object> list)
            {
                var result = new List<Dictionary<string, object>>();
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dictionary)
                    {
                        result.Add(ConvertToDictionary(dictionary));
                    }
                    else
                    {
                        return null; // 数据格式不匹配
                    }
                }
                return result;
            }
            return null; // 数据格式不匹配
        }

        [Serializable]
        private class Serialization<T>
        {
            public List<T> items;
            public Serialization(List<T> items)
            {
                this.items = items;
            }

            public List<T> ToList()
            {
                return items;
            }
        }
    }
}
