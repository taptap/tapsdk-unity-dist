using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.IO;
using UnityEngine;
using TapSDK.Core;

namespace TapSDK.Core.Standalone.Internal {
    public class Prefs {
        internal static readonly string OLD_PERSISTENT_FILE_NAME = "tapdb_storage_v2";

        private string persistentFilePath;

        private readonly ConcurrentDictionary<string, object> data;

        private readonly Thread persistThread;

        private readonly AutoResetEvent persistEvent;

        public Prefs() {
            string newCacheFileName = OLD_PERSISTENT_FILE_NAME;
            if( TapTapSDK.taptapSdkOptions != null && !string.IsNullOrEmpty(TapTapSDK.taptapSdkOptions.clientId)) {
                newCacheFileName = OLD_PERSISTENT_FILE_NAME + "_" + TapTapSDK.taptapSdkOptions.clientId;
            }
            persistentFilePath = Path.Combine(Application.persistentDataPath, newCacheFileName);
            // 兼容旧版缓存文件
            if( !File.Exists(persistentFilePath)) {
                string oldPath = Path.Combine(Application.persistentDataPath, OLD_PERSISTENT_FILE_NAME);
                if (File.Exists(oldPath)){
                    File.Move(oldPath, persistentFilePath);
                }
            }
            if (File.Exists(persistentFilePath)) {
                try {
                    string json = File.ReadAllText(persistentFilePath);
                    Dictionary<string, object> jsonData = Json.Deserialize(json) as Dictionary<string, object>;
                    data = new ConcurrentDictionary<string, object>(jsonData);
                } catch (Exception e) {
                    TapLogger.Error(e.Message);
                    File.Delete(persistentFilePath);
                }
            }
            if (data == null) {
                data = new ConcurrentDictionary<string, object>();
            }
            persistEvent = new AutoResetEvent(false);
            persistThread = new Thread(PersistProc) {
                IsBackground = true
            };
            persistThread.Start();
        }

        public T Get<T>(string key) {
            if (data.TryGetValue(key, out object val)) {
                return (T)val;
            }
            return default;
        }

        public void Set<T>(string key, T value) {
            data[key] = value;
            persistEvent.Set();
        }

        public bool TryRemove<T>(string key, out T val) {
            if (data.TryRemove(key, out object v)) {
                val = (T)v;
                persistEvent.Set();
                return true;
            }
            val = default;
            return false;
        }

        public void AddOrUpdate(string key, object addValue, Func<string, object, object> updateValueFactory) {
            data.AddOrUpdate(key, addValue, updateValueFactory);
            persistEvent.Set();
        }

        private void PersistProc() {
            while (true) {
                persistEvent.WaitOne();
                try {
                    Dictionary<string, object> dict = data.ToArray()
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                    string json = Json.Serialize(dict);
                    File.WriteAllText(persistentFilePath, json);
                } catch (Exception e) {
                    TapLogger.Error(e.Message);
                }
            }
        }
    }
}
