using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace TapSDK.Core.Standalone.Internal.Http
{

    /// <summary>
    /// 定义 HTTP 签名相关操作的接口。
    /// </summary>
    public interface ITapHttpSign
    {
        /// <summary>
        /// 获取固定的 HTTP 请求头信息。
        /// </summary>
        /// <returns>返回包含固定请求头的字典。</returns>
        Dictionary<string, string> GetFixedHeaders(string url, HttpMethod method, string moduleName, string moduleVersion, bool enableAuthorization);

        /// <summary>
        /// 获取固定的查询参数。
        /// </summary>
        /// <returns>返回包含固定查询参数的字典。</returns>
        Dictionary<string, string> GetFixedQueryParams();

        /// <summary>
        /// 对 HTTP 请求数据进行签名处理。
        /// </summary>
        /// <param name="signData">包含请求数据的 <see cref="TapHttpSignData"/> 对象。</param>
        void Sign(HttpRequestMessage signData);
    }

    public class TapHttpSign
    {
        public static ITapHttpSign CreateDefaultSign()
        {
            return new Default();
        }

        public static ITapHttpSign CreateNoneSign()
        {
            return new None();
        }

        private class Default : ITapHttpSign
        {
            public Dictionary<string, string> GetFixedHeaders(string url, HttpMethod method, string moduleName, string moduleVersion, bool enableAuthorization)
            {
                _ = method ?? throw new ArgumentNullException(nameof(method));
                _ = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
                _ = moduleVersion ?? throw new ArgumentNullException(nameof(moduleVersion));

                if (method == HttpMethod.Post || method == HttpMethod.Get)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>
                    {
                        { "X-Tap-PN", "TapSDK" },
                        { "X-Tap-Device-Id", TapHttpUtils.GenerateDeviceId()},
                        { "X-Tap-Platform", "PC"},
                        { "X-Tap-SDK-Module", moduleName},
                        { "X-Tap-SDK-Module-Version", moduleVersion},
                        { "X-Tap-SDK-Artifact", "Unity"},
                        { "X-Tap-Ts", TapHttpUtils.GenerateTime()},
                        { "X-Tap-Nonce", TapHttpUtils.GenerateNonce()},
                        { "X-Tap-Lang", TapHttpUtils.GenerateLanguage()},
                        { "User-Agent", TapHttpUtils.GenerateUserAgent()},
                    };
                    string currentUserId = TapCoreStandalone.User?.Id;
                    if(currentUserId != null && currentUserId.Length > 0) {
                        headers.Add("X-Tap-SDK-Game-User-Id", currentUserId);
                    }
                    if (enableAuthorization)
                    {
                        string authorization = TapHttpUtils.GenerateAuthorization(url, method.ToString());
                        if (authorization != null)
                        {
                            headers.Add("Authorization", authorization);
                        }
                    }
                    return headers;
                }
                return null;
            }

            public Dictionary<string, string> GetFixedQueryParams()
            {
                return new Dictionary<string, string>
                {
                    { "client_id", TapCoreStandalone.coreOptions.clientId }
                };
            }

            public async void Sign(HttpRequestMessage requestMessage)
            {
                _ = requestMessage ?? throw new ArgumentNullException(nameof(requestMessage));

                string clientToken = TapCoreStandalone.coreOptions.clientToken;
                string methodPart = requestMessage.Method.Method;
                string urlPathAndQueryPart = requestMessage.RequestUri.PathAndQuery;
                var headerKeys = requestMessage.Headers
                    .Where(h => h.Key.StartsWith("x-tap-", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(h => h.Key.ToLowerInvariant())
                    .Select(h => $"{h.Key.ToLowerInvariant()}:{string.Join(",", h.Value)}")
                    .ToList();
                string headersPart = string.Join("\n", headerKeys);
                string bodyPart = string.Empty;
                if (requestMessage.Content != null)
                {
                    bodyPart = await requestMessage.Content.ReadAsStringAsync();
                }
                string signParts = methodPart + "\n" + urlPathAndQueryPart + "\n" + headersPart + "\n" + bodyPart + "\n";
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(clientToken)))
                {
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signParts));
                    string sign = Convert.ToBase64String(hash);
                    requestMessage.Headers.Add("X-Tap-Sign", sign);
                }
            }
        }

        private class None : ITapHttpSign
        {
            public Dictionary<string, string> GetFixedHeaders(string url, HttpMethod method, string moduleName, string moduleVersion, bool enableAuthorization)
            {
                return null;
            }

            public Dictionary<string, string> GetFixedQueryParams()
            {
                return null;
            }

            public void Sign(HttpRequestMessage signData)
            {
                // do nothing
            }
        }
    }
}