using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Core.Standalone.Internal.Openlog
{
    public class TapAppDurationStandalone
    {
        private const string MODULE_NAME = "app_duration";

        private static readonly TapLog log = new TapLog("AppDuration");

        public static void Enable()
        {
#if UNITY_STANDALONE
            // log.Log("Enable");
            List<string> modules = new List<string> { MODULE_NAME };
            TapOpenlogWrapper.TdkEnableModules(JsonConvert.SerializeObject(modules));
#endif
        }

        public static void Disable()
        {
#if UNITY_STANDALONE
            // log.Log("Disable");
            List<string> modules = new List<string> { MODULE_NAME };
            TapOpenlogWrapper.TdkDisableModules(JsonConvert.SerializeObject(modules));
#endif
        }

        public static void SetExtraAppDurationParams(Dictionary<string, string> param)
        {
#if UNITY_STANDALONE
            log.Log("SetExtraAppDurationParams param: ", JsonConvert.SerializeObject(param));
            if (param == null)
            {
                log.Warning("SetExtraAppDurationParams param is null");
                return;
            }
            TapOpenlogWrapper.TdkSetExtraAppDurationParams(JsonConvert.SerializeObject(param));
#endif
        }

        public static void OnLogin(string openId)
        {
#if UNITY_STANDALONE
            log.Log("OnLogin openId: " + openId);
            if (openId == null)
            {
                log.Warning("OnLogin openId is null");
                return;
            }
            Dictionary<string, string> userInfo = new Dictionary<string, string>
            {
                {TapOpenlogParamConstants.PARAM_OPEN_ID, openId}
            };
            TapOpenlogWrapper.TdkOnLogin(JsonConvert.SerializeObject(userInfo));
#endif
        }

        public static void OnLogout()
        {
#if UNITY_STANDALONE
            log.Log("OnLogout");
            TapOpenlogWrapper.TdkOnLogout();
#endif
        }

        public static void OnForeground()
        {
#if UNITY_STANDALONE
            log.Log("OnForeground");
            TapOpenlogWrapper.TdkOnForeground();
#endif
        }

        public static void OnBackground()
        {
#if UNITY_STANDALONE
            log.Log("OnBackground");
            TapOpenlogWrapper.TdkOnBackground();
#endif
        }

        public static string Version()
        {

            string v = null;
#if UNITY_STANDALONE
            try
            {
                v = TapOpenlogWrapper.GetTdkVersion();
            }
            catch (Exception e)
            {
                log.Error("GetVersion error: " + e.Message);
            }
            log.Log("Version: " + v);
#endif
            return v;
        }

        public static string GitCommit()
        {
            string v = null;
#if UNITY_STANDALONE
            try
            {
                v = TapOpenlogWrapper.GetTdkGitCommit();
            }
            catch (Exception e)
            {
                log.Error("GitCommit error: " + e.Message);
            }
            log.Log("GitCommit: " + v);
#endif
            return v;
        }
    }
}