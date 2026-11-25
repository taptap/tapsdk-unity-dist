using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using TapSDK.Core.Internal.Utils;
using UnityEngine;
using TapSDK.Core;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Core.Standalone.Internal {
    public class Prefs {
        internal static readonly string OLD_PERSISTENT_FILE_NAME = "tapdb_storage_v2";

        private string persistentFilePath;

        private readonly ConcurrentDictionary<string, object> data = new ConcurrentDictionary<string, object>();

        private readonly Thread persistThread;

        private readonly AutoResetEvent persistEvent;
        private readonly object mutationLock = new object();
        private readonly HashSet<string> dirtyKeys = new HashSet<string>();
        private readonly SharedCacheStorage storage;
        private readonly bool isolated = TapEventStorage.IsIsolated;
        private readonly object initializationLock = new object();
        private Task initialization;

        public Prefs() {
            string newCacheFileName = OLD_PERSISTENT_FILE_NAME;
            if( TapTapSDK.taptapSdkOptions != null && !string.IsNullOrEmpty(TapTapSDK.taptapSdkOptions.clientId)) {
                newCacheFileName = OLD_PERSISTENT_FILE_NAME + "_" + TapTapSDK.taptapSdkOptions.clientId;
            }
            if (isolated) newCacheFileName = TapEventStorage.PrefsFile;
            persistentFilePath = Path.Combine(TapEventStorage.DataPath ?? Application.persistentDataPath, newCacheFileName);
            storage = new SharedCacheStorage(
                isolated ? persistentFilePath : Path.Combine(Application.persistentDataPath, OLD_PERSISTENT_FILE_NAME),
                persistentFilePath, false);
            initialization = LoadAsync();
            persistEvent = new AutoResetEvent(false);
            persistThread = new Thread(PersistProc) {
                IsBackground = true
            };
            persistThread.Start();
        }

        internal Task EnsureReadyAsync() {
            lock (initializationLock) {
                if (initialization == null || initialization.IsFaulted || initialization.IsCanceled)
                    initialization = LoadAsync();
                return initialization;
            }
        }

        private async Task LoadAsync() {
            await storage.PrepareAsync().ConfigureAwait(false);
            using (isolated ? null : await SharedCacheFile.AcquireAsync(persistentFilePath).ConfigureAwait(false)) {
                Dictionary<string, object> loaded;
                if (!File.Exists(persistentFilePath)) loaded = new Dictionary<string, object>();
                else {
                    // Read errors are not corrupt JSON and must not delete a valid cache.
                    string text = File.ReadAllText(persistentFilePath);
                    try {
                        loaded = Json.Deserialize(text) as Dictionary<string, object>
                            ?? throw new InvalidDataException("[TapDB] Invalid state cache.");
                    } catch (Exception e) {
                        TapLog.Error(e.Message);
                        File.Delete(persistentFilePath);
                        loaded = new Dictionary<string, object>();
                    }
                }
                lock (mutationLock) {
                    foreach (var entry in loaded)
                        if (!dirtyKeys.Contains(entry.Key)) data[entry.Key] = entry.Value;
                }
            }
        }

        public T Get<T>(string key) {
            if (data.TryGetValue(key, out object val)) {
                return (T)val;
            }
            return default;
        }

        public void Set<T>(string key, T value) {
            lock (mutationLock) {
                data[key] = value;
                dirtyKeys.Add(key);
                persistEvent.Set();
            }
        }

        public bool TryRemove<T>(string key, out T val) {
            lock (mutationLock) {
                if (data.TryRemove(key, out object v)) {
                    val = (T)v;
                    dirtyKeys.Add(key);
                    persistEvent.Set();
                    return true;
                }
                val = default;
                return false;
            }
        }

        public void AddOrUpdate(string key, object addValue, Func<string, object, object> updateValueFactory) {
            lock (mutationLock) {
                data.AddOrUpdate(key, addValue, updateValueFactory);
                dirtyKeys.Add(key);
                persistEvent.Set();
            }
        }

        // Shared files are read under the file lock; private files have one persistence worker.
        // Do not reuse this process's startup snapshot when merging pending changes.
        private Dictionary<string, object> ReadLatest() {
            if (!File.Exists(persistentFilePath)) return new Dictionary<string, object>();
            return Json.Deserialize(File.ReadAllText(persistentFilePath)) as Dictionary<string, object>
                ?? throw new InvalidDataException("[TapDB] Invalid state cache.");
        }

        private void PersistProc() {
            bool retry = false;
            while (true) {
                persistEvent.WaitOne(retry ? 1000 : Timeout.Infinite);
                retry = false;
                var updates = new Dictionary<string, object>();
                var removals = new HashSet<string>();
                try {
                    EnsureReadyAsync().GetAwaiter().GetResult(); // Dedicated IO thread, never the Unity context.
                    lock (mutationLock) {
                        foreach (string key in dirtyKeys) {
                            if (data.TryGetValue(key, out object value)) updates[key] = value;
                            else removals.Add(key);
                        }
                        dirtyKeys.Clear();
                    }
                    if (updates.Count == 0 && removals.Count == 0) continue;
                    using (isolated ? null : SharedCacheFile.Acquire(persistentFilePath)) {
                        var latest = ReadLatest();
                        foreach (var update in updates) latest[update.Key] = update.Value;
                        foreach (string key in removals) latest.Remove(key);
                        SharedCacheFile.WriteTextUnderLock(persistentFilePath, Json.Serialize(latest));
                    }
                } catch (Exception e) {
                    lock (mutationLock) {
                        // Retry the latest in-memory values, even if there is no further foreground mutation.
                        dirtyKeys.UnionWith(updates.Keys);
                        dirtyKeys.UnionWith(removals);
                    }
                    retry = true;
                    TapLog.Error(e.Message);
                }
            }
        }
    }
}
