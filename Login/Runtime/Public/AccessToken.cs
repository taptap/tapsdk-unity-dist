using System;
using System.Collections.Generic;
using TapSDK.Core;
using Newtonsoft.Json;

namespace TapSDK.Login
{
    public class AccessToken
    {
        [JsonProperty("kid")]
        public string kid;

        [JsonProperty("token_type")]
        public string tokenType;

        [JsonProperty("mac_key")]
        public string macKey;

        [JsonProperty("mac_algorithm")]
        public string macAlgorithm;

        [JsonProperty("scope")]
        public HashSet<string> scopeSet;

        public AccessToken(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            var dic = Json.Deserialize(json) as Dictionary<string, object>;
            ConstructFromDict(dic);
        }

        public AccessToken(Dictionary<string, object> dic)
        {
            ConstructFromDict(dic);
        }

        public AccessToken()
        {
            
        }

        public Dictionary<string, string> ToDict()
        {
            return new Dictionary<string, string>
            {
                ["kid"] = kid,
                ["token_type"] = tokenType,
                ["mac_key"] = macKey,
                ["mac_algorithm"] = macAlgorithm,
                ["scope"] = string.Join(" ", scopeSet)
            };
        }

        public String ToJson() => Json.Serialize(ToDict());

        private void ConstructFromDict(Dictionary<string, object> dic)
        {
            kid = SafeDictionary.GetValue<string>(dic, "kid");
            tokenType = SafeDictionary.GetValue<string>(dic, "token_type");
            macKey = SafeDictionary.GetValue<string>(dic, "mac_key");
            macAlgorithm = SafeDictionary.GetValue<string>(dic, "mac_algorithm");
            string scopeStr = SafeDictionary.GetValue<string>(dic, "scope");
            if (string.IsNullOrEmpty(scopeStr))
            {
                scopeSet = new HashSet<string>();
            }
            else
            {
                try
                {
                    scopeSet = new HashSet<string>(scopeStr.Split(' '));
                }
                catch (Exception e)
                {
                    scopeSet = new HashSet<string>();
                }
            }
        }
    }
}