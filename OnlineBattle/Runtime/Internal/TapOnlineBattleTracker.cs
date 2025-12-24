using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TapSDK.Core;

namespace TapSDK.OnlineBattle
{
    internal class TapOnlineBattleTracker
    {
        private const string ACTION_INIT = "init";
        private const string ACTION_START = "start";
        private const string ACTION_SUCCESS = "success";
        private const string ACTION_FAIL = "fail";

        private static TapOnlineBattleTracker instance;

        public static TapOnlineBattleTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapOnlineBattleTracker();
                }
                return instance;
            }
        }

        internal void TrackInit()
        {
            ReportLog(ACTION_INIT);
        }

        internal void TrackStart(string funcName, string seesionId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcName },
                { "session_id", seesionId },
            };
            ReportLog(
                ACTION_START,
                new Dictionary<string, string>()
                {
                    { "args", JsonConvert.SerializeObject(parameters) },
                }
            );
        }

        internal void TrackSuccess(string funcName, string sessionId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcName },
                { "session_id", sessionId },
            };
            ReportLog(
                ACTION_SUCCESS,
                new Dictionary<string, string>()
                {
                    { "args", JsonConvert.SerializeObject(parameters) },
                }
            );
        }

        internal void TrackFailure(
            string funcName,
            string sessionId,
            int errorCode = -1,
            string errorMessage = null
        )
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcName },
                { "session_id", sessionId },
                { "error_code", errorCode.ToString() },
                { "error_msg", errorMessage },
            };
            ReportLog(
                ACTION_FAIL,
                new Dictionary<string, string>()
                {
                    { "args", JsonConvert.SerializeObject(parameters) },
                }
            );
        }

        private void ReportLog(string action, Dictionary<string, string> parameters = null)
        {
            TapOnlineBattleUtils.RunOnMainThread(() =>
            {
                TapTapSDK.SendOpenLog(
                    "TapOnlineBattle",
                    TapTapOnlineBattle.Version,
                    action,
                    parameters
                );
            });
        }
    }
}
