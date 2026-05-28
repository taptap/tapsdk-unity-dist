
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private string persistentDataPath = Application.persistentDataPath;

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

        public EventSender()
        {
            // 设置计时器
            timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(SendInterval));
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

            // 加载未发送的事件
            LoadEvents();
            SendEventsAsync(null);
        }

        public async void SendEventsAsync(Action onSendComplete)
        {
            if (eventQueue.IsEmpty)
            {
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
            while (eventsToSend.Count < MaxBatchSize && eventQueue.TryDequeue(out var item))
            {
                eventsToSend.Add(item);
            }

            // 并发 TryDequeue 可能导致本次实际取到 0 条
            if (eventsToSend.Count == 0)
            {
                Interlocked.Exchange(ref _isSending, 0);
                onSendComplete?.Invoke();
                return;
            }

            // 登记 in-flight 批次，SaveEvents 落盘时合并，防止崩溃丢失
            _inFlightBatch = eventsToSend;
            try
            {
                var body = new Dictionary<string, object> {
                    { "data", eventsToSend }
                };

                var response = await tapHttp.PostJsonAsync<Boolean>(path: $"{domain}/v2/batch", json: body);
                if (!response.IsSuccess)
                {
                    log.Warning("Failed to send events");
                    foreach (var eventParams in eventsToSend)
                    {
                        eventQueue.Enqueue(eventParams);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warning($"SendEventsAsync exception: {ex.ToString()}");
                foreach (var eventParams in eventsToSend)
                {
                    eventQueue.Enqueue(eventParams);
                }
            }
            finally
            {
                // 先清 in-flight 再落盘：此时事件已回队（失败）或确认完成（成功）
                _inFlightBatch = null;
                Interlocked.Exchange(ref _isSending, 0);
                SaveEvents();
                // 回调放在持久化之后，隔离外部异常不影响落盘
                try { onSendComplete?.Invoke(); } catch (Exception ex) { log.Warning($"onSendComplete exception: {ex.Message}"); }
            }
        }

        public void Send(Dictionary<string, object> eventParams)
        {
            // 将事件添加到队列
            eventQueue.Enqueue(eventParams);
            SaveEvents();

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
            if (TapTapSDK.taptapSdkOptions != null 
            && !string.IsNullOrEmpty(TapTapSDK.taptapSdkOptions.clientId)){
                return "events_" + TapTapSDK.taptapSdkOptions.clientId + ".json";
            }
            return OldEventFilePath;
        }

        private void LoadEvents()
        {   
            string filePath = Path.Combine(persistentDataPath, GetEventCacheFileName());
            if(!File.Exists(filePath)){
                string oldFilePath = Path.Combine(persistentDataPath, OldEventFilePath);
                // 兼容旧版本文件
                if (File.Exists(oldFilePath)) {
                    File.Move(oldFilePath, filePath);
                }
            }
           
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonData))
                {
                    return;
                }
                var savedEvents = ConvertToListOfDictionaries(Json.Deserialize(jsonData));
                if (savedEvents == null)
                {
                    return;
                }
                foreach (var eventParams in savedEvents)
                {
                    eventQueue.Enqueue(eventParams);
                }
            }
        }

        private void SaveEvents()
        {
            try
            {
                if (eventQueue == null)
                {
                    return;
                }

                // 合并 in-flight 批次：若进程在 HTTP 确认前崩溃，重启后可从磁盘恢复这批事件
                var eventList = new List<Dictionary<string, object>>(eventQueue.ToArray());
                var inFlight = _inFlightBatch;
                if (inFlight != null) eventList.AddRange(inFlight);
                string jsonData = Json.Serialize(eventList);

                if (string.IsNullOrEmpty(GetEventCacheFileName()))
                {
                    TapLog.Error("EventFilePath is null or empty");
                    return;
                }

                string filePath = Path.Combine(persistentDataPath, GetEventCacheFileName());

                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                TapLog.Error("SaveEvents Exception - " + ex.Message);
            }
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