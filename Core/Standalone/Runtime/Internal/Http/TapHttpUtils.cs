using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone.Internal.Service;
using UnityEngine;

namespace TapSDK.Core.Standalone.Internal.Http
{
    public static class TapHttpTime
    {
        private static int timeOffset = 0;
        private static void SetTimeOffset(int offset)
        {
            timeOffset = offset;
        }

        // 获取当前时间的秒级时间戳
        public static int GetCurrentTime()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.UtcNow - epochStart;
            return (int)timeSpan.TotalSeconds + timeOffset;
        }

        public static void FixTime(int time)
        {
            if (time == 0)
            {
                return;
            }
            SetTimeOffset(time - (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
        }


        // 服务端同步的时间戳
        private static long LastServerTime = 0;
        // 设置服务端时间时，当前应用启动时间
        private static long LastStartUpTime = 0 ;
        internal static void ResetLastServerTime(long time){
            LastServerTime = time;
            LastStartUpTime =  (long) Time.realtimeSinceStartup;
        }

        /// <summary>
        /// 根据服务端时间获取当前时间戳，单位：秒
        /// </summary>
        /// <returns>当前时间戳，当服务端时间未设置过时，返回值为 0</returns>
        public static long GetCurrentServerTime(){
            if(LastServerTime == 0){
                return 0;
            }
            long startUpTime = (long) Time.realtimeSinceStartup;
            return LastServerTime + startUpTime - LastStartUpTime;
        }
    }

    public static class TapHttpUtils
    {

        private static readonly TapLog tapLog = new TapLog("Http");

        internal static string GenerateNonce()
        {
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] nonce = new char[10];
            for (int i = 0; i < 10; i++)
            {
                nonce[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return new string(nonce);
        }

        public static string GenerateUserAgent()
        {
            return $"TapSDK-Unity/{TapTapSDK.Version}";
        }

        internal static string GenerateTime()
        {
            return TapHttpTime.GetCurrentTime().ToString();
        }

        internal static string GenerateLanguage()
        {
            return Tracker.getServerLanguage();
        }

        internal static string GenerateDeviceId()
        {
            return SystemInfo.deviceUniqueIdentifier;
        }

        internal static string GenerateAuthorization(string url, string method)
        {
            Type interfaceType = typeof(ITapLoginService);
            Type[] initTaskTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asssembly =>
                {
                    string fullName = asssembly.GetName().FullName;
                    return fullName.StartsWith("TapSDK.Login.Standalone.Runtime");
                })
                .SelectMany(assembly => assembly.GetTypes())
                .Where(clazz =>
                {
                    return interfaceType.IsAssignableFrom(clazz) && clazz.IsClass;
                })
                .ToArray();
            if (initTaskTypes.Length != 1)
            {
                return null;
            }
            try
            {
                ITapLoginService tapLoginService = Activator.CreateInstance(initTaskTypes[0]) as ITapLoginService;
                string authorization = tapLoginService.ObtainAuthorizationAsync(url, method);
                return authorization;
            }
            catch (Exception e)
            {
                TapLog.Error("e = " + e);
            }
            return null;
        }

        public static void PrintRequest(HttpClient client, HttpRequestMessage request)
        {
            if (TapLogger.LogDelegate == null)
            {
                return;
            }
            if (client == null)
            {
                return;
            }
            if (request == null)
            {
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== HTTP Request Start ===");
            sb.AppendLine($"URL: {request.RequestUri}");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"Headers: ");
            foreach (var header in client.DefaultRequestHeaders)
            {
                sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
            }
            foreach (var header in request.Headers)
            {
                sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
            }
            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    sb.AppendLine($"\t{header.Key}: {string.Join(",", header.Value.ToArray())}");
                }
            }
            string contentString = null;
            try
            {
                contentString = request.Content.ReadAsStringAsync().Result;
            }
            catch (Exception)
            {
            }
            if (!string.IsNullOrEmpty(contentString))
            {
                sb.AppendLine($"Content: \n{contentString}");
            }
            sb.AppendLine("=== HTTP Request End ===");
            tapLog.Log($"HTTP Request [{request.RequestUri.PathAndQuery}]", sb.ToString());
        }

        public static void PrintResponse(HttpResponseMessage response)
        {
            if (TapLogger.LogDelegate == null)
            {
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== HTTP Response Start ===");
            sb.AppendLine($"URL: {response.RequestMessage.RequestUri}");
            sb.AppendLine($"Status Code: {response.StatusCode}"); 
            string contentString = null;
            try
            {
                contentString = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception)
            {
            }
            if (!string.IsNullOrEmpty(contentString))
            {
                sb.AppendLine($"Content: {contentString}");
            }
            sb.AppendLine("=== HTTP Response End ===");
            tapLog.Log($"HTTP Response [{response.RequestMessage.RequestUri.PathAndQuery}]", sb.ToString());
        }

    }
}