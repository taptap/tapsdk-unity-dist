using System;
using System.Security.Cryptography;
using System.Text;
using Random = System.Random;

namespace TapSDK.Login.Internal
{
    public static class CodeUtil
    {
        public static string GenerateCodeVerifier()
        {
            const string baseStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
            var random = new Random();
            var builder = new StringBuilder();
            int l = baseStr.Length;
            for (var i = 0; i < 128; i++)
            {
                var number = random.Next(l);
                builder.Append(baseStr[number]);
            }

            return builder.ToString();
        }

        public static string GetCodeChallenge(string codeVerifier)
        {
            return Base64UrlEncodeNoPadding(SHA256(codeVerifier));
        }

        private static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            var base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        private static byte[] SHA256(string inputStirng)
        {
            var bytes = Encoding.ASCII.GetBytes(inputStirng);
            var sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }
    }
}