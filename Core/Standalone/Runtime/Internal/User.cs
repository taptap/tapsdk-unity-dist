using System.Collections.Generic;

namespace TapSDK.Core.Standalone.Internal {
    public class User {
        internal static readonly string USER_ID_KEY = "tapdb_played_duration_user_id";

        internal string Id {
            get => id;
            set {
                id = value;
                TapCoreStandalone.Prefs.Set(USER_ID_KEY, id);
            }
        }

        private string id;

        private readonly PlayRecorder playRecorder;

        internal User() {
            playRecorder = new PlayRecorder();
        }

        internal void Login(string userId, Dictionary<string, object> props = null) {
            // 先执行旧用户登出逻辑
            Id = TapCoreStandalone.Prefs.Get<string>(USER_ID_KEY);
            if (!string.IsNullOrWhiteSpace(Id)) {
                Logout();
            }

            // 再执行新用户登录逻辑
            Id = userId;

            TapEventStandalone.Tracker?.TrackEvent(Constants.USER_LOGIN, props, true);
            

            Dictionary<string, object> updateProps = new Dictionary<string, object> {
                { "has_user", true },
            };
            TapEventStandalone.Tracker?.TrackDeviceProperties(Constants.PROPERTY_UPDATE_TYPE, updateProps);

            playRecorder.Start();
        }

        internal void Logout() {
            playRecorder.Stop();

            Id = null;
        }
    }
}