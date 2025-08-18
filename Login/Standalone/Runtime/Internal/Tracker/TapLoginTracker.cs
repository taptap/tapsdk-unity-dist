using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Openlog;

namespace TapSDK.Login.Standalone.Internal
{
    internal class TapLoginTracker
    {

        private const string ACTION_INIT = "init";
        private const string ACTION_START = "start";
        private const string ACTION_SUCCESS = "success";
        private const string ACTION_FAIL = "fail";
        private const string ACTION_CANCEL = "cancel";

        internal static string LOGIN_TYPE_CLIENT = "pc_client";
        internal static string LOGIN_TYPE_CODE = "pc_code";
        internal static string LOGIN_TYPE_BROWSER = "pc_browser";

        private static TapLoginTracker instance;

        private TapOpenlogStandalone openlog;

        private TapLoginTracker()
        {
            openlog = new TapOpenlogStandalone("TapLogin", TapTapLogin.Version);
        }

        public static TapLoginTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapLoginTracker();
                }
                return instance;
            }
        }

        internal void TrackInit()
        {
            ReportLog(ACTION_INIT);
        }

        internal void TrackStart(string funcNace, string seesionId, string loginType = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
            };
            if (loginType != null) {
                parameters["login_type"] = loginType;
            }
            ReportLog(ACTION_START, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackSuccess(string funcNace, string seesionId, string loginType)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
                { "login_type", loginType }
            };
            ReportLog(ACTION_SUCCESS, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackCancel(string funcNace, string seesionId, string loginType = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
            };
            if (loginType != null) {
                parameters["login_type"] = loginType;
            }
            ReportLog(ACTION_CANCEL, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackFailure(string funcNace, string seesionId, string loginType, int errorCode = -1, string errorMessage = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
                { "error_code", errorCode.ToString() },
                { "error_msg", errorMessage },
                { "login_type", loginType }
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