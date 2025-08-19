using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using TapSDK.Login.Internal.Http;
using TapSDK.Core.Standalone.Internal.Http;
using TapSDK.Login.Standalone.Internal.Http;

namespace TapSDK.Login.Internal
{
    public static class LoginService
    {
        private static readonly TapHttp tapHttp = TapHttp
            .NewBuilder("TapLogin", TapTapLogin.Version)
            .Sign(new TapLoginSign())
            .Build();

        public static async Task<QRCodeData> GetQRCodeUrl(string clientId, string[] scopes)
        {
            Dictionary<string, string> data = new Dictionary<string, string> {
                { "client_id", clientId },
                { "response_type", "device_code" },
                { "scope", string.Join(",", scopes) },
                { "version", TapTapLogin.Version },
                { "platform", "unity" },
                { "info", "{\"device_id\":\"" + SystemInfo.deviceModel + "\"}" }
            };
            TapHttpResult<QRCodeData> result = await tapHttp.PostFormAsync<QRCodeData>(TapTapSdk.CurrentRegion.CodeUrl(), form: data);
            if (result.IsSuccess)
            {
                return result.Data;
            }
            else
            {
                throw result.HttpException;
            }
        }

        public static async Task<TokenData> Authorize(string clientId, string code)
        {
            Dictionary<string, string> data = new Dictionary<string, string> {
                { "client_id", clientId },
                { "grant_type", "authorization_code" },
                { "secret_type", "hmac-sha-1" },
                { "code", code },
                { "redirect_uri", WebLoginRequestManager.Instance.GetCurrentRequest().GetRedirectUri() },
                { "code_verifier", WebLoginRequestManager.Instance.GetCodeVerifier() }
            };
            TapHttpResult<TokenData> result = await tapHttp.PostFormAsync<TokenData>(TapTapSdk.CurrentRegion.TokenUrl(), form: data);
            if (result.IsSuccess)
            {
                return result.Data;
            }
            else
            {
                throw result.HttpException;
            }
        }

         public static async Task<TokenData> Authorize(string clientId, string code, string codeVerify) {
            Dictionary<string, string> data = new Dictionary<string, string> {
                { "client_id", clientId },
                { "grant_type", "authorization_code" },
                { "secret_type", "hmac-sha-1" },
                { "code", code },
                { "redirect_uri", "tapoauth://authorize" },
                { "code_verifier", codeVerify }
            };
            TapHttpResult<TokenData> result = await tapHttp.PostFormAsync<TokenData>(TapTapSdk.CurrentRegion.TokenUrl(), form: data);
            if (result.IsSuccess)
            {
                return result.Data;
            }
            else
            {
                throw result.HttpException;
            }
        }

        public static async Task<TokenData> RequestScanQRCodeResult(string clientId, string deviceCode)
        {
            Dictionary<string, string> data = new Dictionary<string, string> {
                { "grant_type", "device_token" },
                { "client_id", clientId },
                { "secret_type", "hmac-sha-1" },
                { "code", deviceCode },
                { "version", "1.0" },
                { "platform", "unity" },
                { "info", "{\"device_id\":\"" + SystemInfo.deviceModel + "\"}" }
            };
            TapHttpResult<TokenData> result = await tapHttp.PostFormAsync<TokenData>(TapTapSdk.CurrentRegion.TokenUrl(), form: data);
            if (result.IsSuccess)
            {
                return result.Data;
            }
            else
            {
                throw result.HttpException;
            }
        }

        public static async Task<ProfileData> GetProfile(string clientId, AccessToken token, int timestamp = 0)
        {
            string url = TapTapSdk.CurrentRegion.ProfileUrl(token.scopeSet.Contains(TapTapLogin.TAP_LOGIN_SCOPE_PUBLIC_PROFILE)) + clientId;
            var uri = new Uri(url);
            var ts = timestamp;
            if (ts == 0)
            {
                var dt = DateTime.UtcNow - new DateTime(1970, 1, 1);
                ts = (int)dt.TotalSeconds;
            }
            var sign = "MAC " + GetAuthorizationHeader(token.kid,
                token.macKey,
                token.macAlgorithm,
                "GET",
                uri.PathAndQuery,
                uri.Host,
                "443", ts);
            Dictionary<string, string> headers = new Dictionary<string, string> {
                { "Authorization", sign }
            };

            TapHttpResult<ProfileData> result = await tapHttp.GetAsync<ProfileData>(url, headers: headers);
            if(result.IsSuccess)
            {
                return result.Data;
            }
            else
            {
                throw result.HttpException;
            }   
        }

        public static string GetAuthorizationHeader(string kid,
            string macKey,
            string macAlgorithm,
            string method,
            string uri,
            string host,
            string port,
            int timestamp)
        {
            var nonce = new System.Random().Next().ToString();

            var normalizedString = $"{timestamp}\n{nonce}\n{method}\n{uri}\n{host}\n{port}\n\n";

            HashAlgorithm hashGenerator;
            switch (macAlgorithm)
            {
                case "hmac-sha-256":
                    hashGenerator = new HMACSHA256(Encoding.ASCII.GetBytes(macKey));
                    break;
                case "hmac-sha-1":
                    hashGenerator = new HMACSHA1(Encoding.ASCII.GetBytes(macKey));
                    break;
                default:
                    throw new InvalidOperationException("Unsupported MAC algorithm");
            }

            var hash = Convert.ToBase64String(hashGenerator.ComputeHash(Encoding.ASCII.GetBytes(normalizedString)));

            var authorizationHeader = new StringBuilder();
            authorizationHeader.AppendFormat(@"id=""{0}"",ts=""{1}"",nonce=""{2}"",mac=""{3}""",
                kid, timestamp, nonce, hash);

            return authorizationHeader.ToString();
        }

        public static async Task<TokenData> RefreshToken(string clientId, string accessToken)
        {
            Dictionary<string, string> data = new Dictionary<string, string> {
                { "client_id", clientId },
                { "grant_type", "refresh_token" },
                { "token", accessToken },
                { "token_type_hint", "access_token" },
                { "platform", "unity" },
                { "info", "{\"device_id\":\"" + SystemInfo.deviceModel + "\"}" }
            };
            TapHttpResult<TokenData> result = await tapHttp.PostFormAsync<TokenData>(TapTapSdk.CurrentRegion.TokenUrl(), form: data);
            if(result.IsSuccess)
            {
                return result.Data;
            }
            else
            {
                throw result.HttpException;
            }       
        }
    }
}
