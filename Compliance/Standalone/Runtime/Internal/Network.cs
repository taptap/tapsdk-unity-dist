using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TapSDK.Compliance.Internal.Http;
using TapSDK.Compliance.Model;
using TapSDK.Core;
using TapSDK.Login;
using TapSDK.Login.Internal;
using System.Net;


namespace TapSDK.Compliance.Internal
{
    public static class Network {
        static readonly string ChinaHost = "https://tapsdk.tapapis.cn";

        private static ComplianceHttpClient
            HttpClient = new ComplianceHttpClient(ChinaHost);

        private static string gameId;

        private static string clientToken;
        
        internal static void SetGameInfo(string gameId, string clientToken)
        {
            Network.gameId = gameId;
            Network.clientToken = clientToken;
        }

        internal static void InitSetting()
        {
            HttpClient.ChangeAddtionalHeader("Accept-Language","zh-CN");
            string host = ChinaHost;
            if (HttpClient != null)
            {
                Type httpClientType = typeof(ComplianceHttpClient);
                var hostFieldInfo = httpClientType.GetField("serverUrl", BindingFlags.NonPublic | BindingFlags.Instance);
                hostFieldInfo?.SetValue(HttpClient, host);
            }
        }
        
        /// <summary>
        /// 拉取配置并缓存在内存
        /// 没有持久化的原因是无法判断 SDK 自带与本地持久化版本的高低
        /// </summary>
        /// <returns></returns>
        internal static async Task<RealNameConfigResult> FetchConfig(string userId) {
            string path = $"real-name/v1/get-global-config";
            var headers = GetAuthHeaders();
            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId,
                ["user_identifier"] = userId
            };
            RealNameConfigResult response = await HttpClient.Get<RealNameConfigResult>(path, headers, queryParams);
            return response;
        }

        /// <summary>
        /// 拉取实名认证数据
        /// </summary>
        /// <returns></returns>
        internal static async Task<VerificationResult> FetchVerification(string userId) 
        {
            string path = $"real-name/v1/anti-addiction-token";
            var headers = GetAuthHeaders();
            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId,
                ["user_identifier"] = userId
            };
            VerificationResult response = await HttpClient.Get<VerificationResult>(path, headers, queryParams);
            return response;
        }

         /// <summary>
        /// V1 升级 v2 token
        internal static async Task<VerificationResult> UpgradeToken(string userId, string oldToken) 
        {
            string path = $"real-name/v1/anti-addiction-token-upgrade";
            var param = new Dictionary<string, object> {
                ["anti_addiction_token_v1"] = oldToken,
                ["user_identifier"] = userId
            };
            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId
           };
            var headers = GetAuthHeaders();
            VerificationResult response = await HttpClient.Post<VerificationResult>(path, headers, data:param, queryParams);
            return response;
        }

        /// </summary>
        /// 使用 TapToken 获取实名 token
        /// <returns></returns>
        public static async Task<VerificationResult> FetchVerificationByTapToken(string userId, AccessToken token, long timestamp = 0) {
            // 这里手动拼 query 是为了进行 Tap 的签算
            string path = $"real-name/v1/anti-addiction-token-taptap?client_id={gameId}&user_identifier={WebUtility.UrlEncode(userId)}";            
            var httpClientType = typeof(ComplianceHttpClient);
            var hostFieldInfo = httpClientType.GetField("serverUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            string host = hostFieldInfo?.GetValue(HttpClient) as string;
            var uri = new Uri(host  + "/" +  path);

            var sign = GetMacToken(token, uri, timestamp);
            var headers = GetAuthHeaders();
            headers.Add("Authorization", sign);

            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId,
                ["user_identifier"] = userId
            };
            VerificationResult response = await HttpClient.Get<VerificationResult>(path, headers:headers, queryParams);
            return response;
        }
        
        private static string GetMacToken(AccessToken token, Uri uri, long timestamp = 0) {
            TapLogger.Debug(" uri = " + uri.Host + " path = " + uri.PathAndQuery + " token mac = "
             + token.macKey);
            int ts = (int)timestamp;
            if (ts == 0) {
                var dt = DateTime.UtcNow - new DateTime(1970, 1, 1);
                ts = (int)dt.TotalSeconds;
            }
            TapLogger.Debug(" GetMacToken ts = " + ts);
            var sign = "MAC " + LoginService.GetAuthorizationHeader(token.kid,
                token.macKey,
                token.macAlgorithm,
                "GET",
                uri.PathAndQuery,
                uri.Host,
                "443", ts);
            return sign;
        }
        
        /// <summary>
        /// 检测身份信息是否通过
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="idCard">身份证信息</param>
        /// <returns></returns>
        internal static async Task<VerificationResult> FetchVerificationManual(string userName, string idCard)
        {
            var tcs = new TaskCompletionSource<VerificationResult>();
            string path = $"real-name/v1/anti-addiction-token-manual";
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["name"] = userName,
                ["idcard"] = idCard,
                ["user_identifier"] = TapTapComplianceManager.UserId
            };
            var headers = GetAuthHeaders();
            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId
            };
            VerificationResult response = await HttpClient.Post<VerificationResult>(path, headers, data, queryParams);
            tcs.TrySetResult(response);
            
            return await tcs.Task;
        }

        /// <summary>
        /// 获取用户配置
        /// </summary>
        /// <returns></returns>
        internal static async Task<UserComplianceConfigResult> CheckUserConfig() 
        {
            string path = $"anti-addiction/v1/get-config-by-token";
            Dictionary<string, object> headers = GetAuthHeaders();
            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId,
                ["user_identifier"] = TapTapComplianceManager.UserId,
                ["platform"] = "pc"
            };
            UserComplianceConfigResult response = await HttpClient.Get<UserComplianceConfigResult>(path, headers, queryParams);
            #if UNITY_EDITOR
            TapLogger.Debug($"检查用户状态: ageLimit: {response.userState.ageLimit} ageCheck: {response.ageCheckResult.allow}  IsAdult: {response.userState.isAdult} ");
            #endif
            return response;
        }
        /// <summary>
        /// 检测是否可玩
        /// </summary>
        /// <returns></returns>
        internal static async Task<PlayableResult> CheckPlayable() 
        {
            string  path = $"anti-addiction/v1/heartbeat";
            
            Dictionary<string,object> data = new Dictionary<string,object>{
                ["session_id"] = TapTapComplianceManager.CurrentSession,
                ["user_identifier"] = TapTapComplianceManager.UserId
            };
            Dictionary<string, object> headers = GetAuthHeaders();
            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId
            };
            PlayableResult response = await HttpClient.Post<PlayableResult>(path, headers, data, queryParams);
            #if UNITY_EDITOR
            TapLogger.Debug($"检查是否可玩结果: remainTime: {response.RemainTime}  Content: {response.Content}");
            #endif
            return response;
        }

        /// <summary>
        /// 检测是否可充值
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        internal static async Task<PayableResult> CheckPayable(long amount) 
        {
            string path = $"anti-addiction/v1/payable";
            Dictionary<string, object> headers = GetAuthHeaders();
            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId,
                ["user_identifier"] = TapTapComplianceManager.UserId,
                ["amount"] = amount
            };
            PayableResult response = await HttpClient.Get<PayableResult>(path, headers, queryParams);
            return response;
        }

        /// <summary>
        /// 上传充值操作
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        internal static async Task SubmitPayment(long amount) 
        {
            string path = $"anti-addiction/v1/payment-submit";
            
            Dictionary<string, object> data = new Dictionary<string, object> 
            {
                { "amount", amount },
                {"user_identifier", TapTapComplianceManager.UserId}
            };
            Dictionary<string, object> headers = GetAuthHeaders();
            var queryParams = new Dictionary<string, object>{
                ["client_id"] = gameId
            };
           
            await HttpClient.Post<SubmitPaymentResponse>(path, headers, data, queryParams);
        }
        // internal static Dictionary<string, object> GetAuthHeaders(){
        //     return new Dictionary<string,object>{};
        // }

        internal static Dictionary<string, object> GetAuthHeaders() 
        {
            var headers = new Dictionary<string, object>();
            string token = Verification.GetCurrentToken();
            if (!string.IsNullOrEmpty(token)) 
            {
                headers.Add("X-Tap-Anti-Addiction-Token", token);
            }
            return headers;
        }

        internal static void SetTestEnvironment(bool enable) {
            
        }
    }
}
