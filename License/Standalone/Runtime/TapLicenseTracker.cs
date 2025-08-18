using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Openlog;

namespace TapSDK.License.Standalone
{
    internal class TaplicenseTracker
    {
        private const string ACTION_INIT = "init";
        private const string ACTION_START = "start";
        private const string ACTION_SUCCESS = "success";
        private const string ACTION_FAIL = "fail";



        private static TaplicenseTracker instance;

        private TapOpenlogStandalone openlog;

        private TaplicenseTracker()
        {
            openlog = new TapOpenlogStandalone("TapLicense", TapTapLicense.Version);
        }

        public static TaplicenseTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TaplicenseTracker();
                }
                return instance;
            }
        }

        internal void TrackInit()
        {
            ReportLog(ACTION_INIT);
        }
        
         internal void TrackStart(string funcName, string seesionId, Dictionary<string, string> props = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcName },
                { "session_id", seesionId },
            };
            if (props != null) {
               foreach (var item in props)
               {
                    parameters.Add(item.Key, item.Value);
               }
            }
            ReportLog(ACTION_START, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackSuccess(string funcName, string seesionId, Dictionary<string, string> props = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcName },
                { "session_id", seesionId },
            };
            if (props != null) {
               foreach (var item in props)
               {
                    parameters.Add(item.Key, item.Value);
               }
            }
            ReportLog(ACTION_SUCCESS, new Dictionary<string, string>()
            {
                { "args", JsonConvert.SerializeObject(parameters) }
            });
        }

        internal void TrackFailure(string funcName, string seesionId, Dictionary<string, string> props = null, int errorCode = -1, string errorMessage = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "func_name", funcName },
                { "session_id", seesionId },
                { "error_code", errorCode.ToString() },
                { "error_msg", errorMessage }
            };
            if (props != null) {
               foreach (var item in props)
               {
                    parameters.Add(item.Key, item.Value);
               }
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