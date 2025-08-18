using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.Core.Standalone.Internal.Openlog
{
    internal class TapCoreTracker
    {

        private const string ACTION_INIT = "init";
        private const string ACTION_START = "start";
        private const string ACTION_SUCCESS = "success";
        private const string ACTION_FAIL = "fail";
        private const string ACTION_CANCEL = "cancel";

        internal static string SUCCESS_TYPE_RESTART = "restart";
        internal static string SUCCESS_TYPE_INIT = "init";

        internal static string METHOD_LAUNCHER = "isLaunchedFromTapTapPC";

        private static TapCoreTracker instance;

        private TapOpenlogStandalone openlog;

        private TapCoreTracker()
        {
            openlog = new TapOpenlogStandalone("TapSDKCore", TapTapSDK.Version);
        }

        public static TapCoreTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapCoreTracker();
                }
                return instance;
            }
        }

        internal void TrackInit()
        {
            ReportLog(ACTION_INIT);
        }

        internal void TrackStart(string funcNace, string seesionId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
            };
            ReportLog(ACTION_START, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackSuccess(string funcNace, string seesionId, string successType)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
                { "launch_success_type", successType }
            };
            ReportLog(ACTION_SUCCESS, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackCancel(string funcNace, string seesionId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
            };
            ReportLog(ACTION_CANCEL, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackFailure(string funcNace, string seesionId, int errorCode = -1, string errorMessage = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
                { "error_code", errorCode.ToString() },
                { "error_msg", errorMessage }
            };
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