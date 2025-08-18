using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Openlog;

namespace TapSDK.Compliance.Standalone.Internal
{
    internal class TapComplianceTracker
    {

        private const string ACTION_INIT = "init";
        private const string ACTION_START = "start";
        private const string ACTION_SUCCESS = "success";
        private const string ACTION_FAIL = "fail";
        private const string ACTION_CANCEL = "cancel";

        private static TapComplianceTracker instance;

        private TapOpenlogStandalone openlog;

        internal string currentVerifyType;

        private TapComplianceTracker()
        {
            openlog = new TapOpenlogStandalone("TapCompliance", TapTapCompliance.Version);
        }

        public static TapComplianceTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TapComplianceTracker();
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
            currentVerifyType = "fast_verify";
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

        internal void TrackSuccess(string funcNace, string seesionId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcNace },
                { "session_id", seesionId },
                { "identity_verify_type", currentVerifyType }
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
                { "identity_verify_type", currentVerifyType }
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
                { "error_msg", errorMessage },
                { "identity_verify_type", currentVerifyType }
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