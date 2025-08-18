using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal {
    public class PlayRecorder {
        internal static readonly string PLAYED_DURATION_KEY = "tapdb_played_duration";

        /// <summary>
        /// 记录间隔
        /// </summary>
        private const int RECORD_INTERVAL = 2 * 1000;

        private CancellationTokenSource cts;

        /// <summary>
        /// 启动记录
        /// </summary>
        public async void Start() {
            if (cts != null && !cts.IsCancellationRequested) {
                cts.Cancel();
            }
            
            cts = new CancellationTokenSource();
            while (Application.isPlaying) {
                try {
                    await Task.Delay(RECORD_INTERVAL, cts.Token);
                } catch (TaskCanceledException) {
                    break;
                }

                // 保存用户游玩时长
                TapCoreStandalone.Prefs.AddOrUpdate(PLAYED_DURATION_KEY,
                    2L,
                    (k, v) => (long)v + 2);
            }
        }

        /// <summary>
        /// 结束记录并上报
        /// </summary>
        public void Stop() {
            cts?.Cancel();
            if (TapCoreStandalone.Prefs.TryRemove(PLAYED_DURATION_KEY, out long duration)) {
                Dictionary<string, object> props = new Dictionary<string, object> {
                    { "duration", duration }
                };
                TapEventStandalone.Tracker?.TrackEvent("play_game", props, true);
            }
        }
    }
}
