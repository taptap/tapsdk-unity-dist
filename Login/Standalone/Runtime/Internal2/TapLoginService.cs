using System;
using System.Threading.Tasks;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone.Internal.Service;
using TapSDK.Login.Internal;

namespace TapSDK.Login.Standalone.Internal
{
    internal class TapLoginService : ITapLoginService
    {
        public string ObtainAuthorizationAsync(string url, string method)
        {
            TapTapAccount tapAccount = AccountManager.Instance.Account;
            if (tapAccount == null)
            {
                return null;
            }
            AccessToken token = tapAccount.accessToken;
            if (token == null)
            {
                return null;
            }
            var dt = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var ts = (int)dt.TotalSeconds;
            string port = "443";
            if (url.StartsWith("http://"))
            {
                port = "80";
            }
            UriBuilder uri = new UriBuilder(url);
            string host = uri.Host;
            string pathAndQuery = url.Substring(url.LastIndexOf(host) + host.Length);
            var sign = "MAC " + LoginService.GetAuthorizationHeader(
                token.kid,
                token.macKey,
                token.macAlgorithm,
                method,
                pathAndQuery,
                host,
                port,
                ts);
            return sign;
        }
    }
}