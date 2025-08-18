using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Openlog;

namespace TapSDK.Achievement.Standalone.Internal
{
    internal class TapAchievementTracker
    {

        private const string ACTION_INIT = "init";
        private const string ACTION_START = "start";
        private const string ACTION_SUCCESS = "success";
        private const string ACTION_FAIL = "fail";

        private static TapAchievementTracker instance;

        private TapOpenlogStandalone openlog;

        private TapAchievementTracker()
        {
            openlog = new TapOpenlogStandalone("TapAchievement", TapTapAchievement.Version);
        }

        public static TapAchievementTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapAchievementTracker();
                }
                return instance;
            }
        }

        internal void TrackInit()
        {
            ReportLog(ACTION_INIT);
        }

        internal void TrackStart(string funcNace, string seesionId, string achievementId = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
            };
            if (achievementId != null)
            {
                parameters.Add("achievement_id", achievementId);
            }
            ReportLog(ACTION_START, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackSuccess(string funcNace, string seesionId, string achievementId = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId }
            };
            if (achievementId != null)
            {
                parameters.Add("achievement_id", achievementId);
            }
            ReportLog(ACTION_SUCCESS, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackFailure(string funcNace, string seesionId, string achievementId = null, int errorCode = -1, string errorMessage = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
                { "error_code", errorCode.ToString() },
                { "error_msg", errorMessage }
            };
            if (achievementId != null)
            {
                parameters.Add("achievement_id", achievementId);
            }
            ReportLog(ACTION_FAIL, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }


        private void ReportLog(string action, Dictionary<string, string> parameters = null)
        {
            openlog.LogBusiness(action, parameters);
        }
    }
}