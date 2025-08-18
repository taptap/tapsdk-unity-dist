using System.Collections.Generic;
using System.Net.Http;
using TapSDK.Core.Standalone.Internal.Http;

namespace TapSDK.Login.Standalone.Internal.Http
{
    internal class TapLoginSign : ITapHttpSign
    {
        public Dictionary<string, string> GetFixedHeaders(string url, HttpMethod method, string moduleName, string moduleVersion, bool enableAuthorization)
        {
            return new Dictionary<string, string>
            {
                { "User-Agent", TapHttpUtils.GenerateUserAgent()},
            };
        }

        public Dictionary<string, string> GetFixedQueryParams()
        {
            return new Dictionary<string, string>();
        }

        public void Sign(HttpRequestMessage signData)
        {
            // Do nothing
        }
    }
}