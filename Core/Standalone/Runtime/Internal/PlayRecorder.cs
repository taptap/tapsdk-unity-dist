using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using TapSDK.Core.Internal.Log;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal {
    public class PlayRecorder {
        internal static readonly string PLAYED_DURATION_KEY = "tapdb_played_duration";
        internal const string SnapshotKey = "tapdb_duration_snapshot";
        private const int RECORD_INTERVAL = 2 * 1000;
        private CancellationTokenSource cts;

        public async void Start() {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;
            string user = TapCoreStandalone.User.Id;
            string snapshot = null;
            long duration = 0;
            bool warned = false;
            while (Application.isPlaying) {
                try {
                    await Task.Delay(RECORD_INTERVAL, token);
                } catch (TaskCanceledException) {
                    break;
                }
                if (token.IsCancellationRequested) break;
                if (!TapEventStorage.IsIsolated) {
                    TapCoreStandalone.Prefs.AddOrUpdate(PLAYED_DURATION_KEY, 2L, (k, v) => (long)v + 2);
                    continue;
                }
                duration += 2;
                try {
                    Dictionary<string, object> payload;
                    try {
                        payload = TapEventStandalone.Tracker.BuildEvent("play_game",
                            new Dictionary<string, object> { { "duration", duration } }, true);
                        if (token.IsCancellationRequested) break;
                        snapshot = Json.Serialize(payload);
                        warned = false;
                    } catch (Exception e) {
                        if (token.IsCancellationRequested) break;
                        if (!warned) {
                            TapLog.Warning("TapSDK duration context retained: " + e.GetType().Name);
                            warned = true;
                        }
                        payload = snapshot == null
                            ? TapEventStandalone.Tracker.BuildEvent("play_game", null, true, true)
                            : (Dictionary<string, object>)Json.Deserialize(snapshot);
                        payload["user_id"] = user;
                        ((Dictionary<string, object>)payload["properties"])["duration"] = duration;
                        snapshot = Json.Serialize(payload);
                    }
                    // 游戏回调可能在构造事件时重入 ClearUser/SetUserID，保存前再次检查取消状态。
                    if (token.IsCancellationRequested) break;
                    TapCoreStandalone.Prefs.Set(SnapshotKey, snapshot);
                } catch (Exception e) {
                    if (!warned) {
                        TapLog.Warning("TapSDK duration snapshot retained: " + e.GetType().Name);
                        warned = true;
                    }
                }
            }
        }

        public void Stop() {
            cts?.Cancel();
            if (TapEventStorage.IsIsolated) {
                string snapshot = TapCoreStandalone.Prefs.Get<string>(SnapshotKey);
                if (snapshot != null) {
                    // 直接发送已保存的快照，不调用游戏回调，确保身份和时长属于同一段。
                    TapEventStandalone.Tracker.SendSnapshot((Dictionary<string, object>)Json.Deserialize(snapshot));
                    TapCoreStandalone.Prefs.TryRemove<string>(SnapshotKey, out _);
                }
                return;
            }
            if (TapCoreStandalone.Prefs.TryRemove(PLAYED_DURATION_KEY, out long duration)) {
                TapEventStandalone.Tracker?.TrackEvent("play_game",
                    new Dictionary<string, object> { { "duration", duration } }, true);
            }
        }
    }
}
