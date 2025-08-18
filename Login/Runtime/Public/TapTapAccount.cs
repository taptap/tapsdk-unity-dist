using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using TapSDK.Core;
using TapSDK.Login;
using UnityEngine;

namespace TapSDK.Login
{
    public class TapTapAccount
    {
        [JsonProperty("access_token")]
        public AccessToken accessToken { get; }
        
        [JsonProperty("openid")]
        public string openId { get; }
        
        [JsonProperty("unionid")]
        public string unionId { get; }
        
        [JsonProperty("name")]
        [CanBeNull] public string name { get; }
        
        [JsonProperty("avatar")]
        [CanBeNull] public string avatar { get; }
        
        [JsonProperty("email")]
        [CanBeNull] public string email { get; }

        public TapTapAccount(Dictionary<string, object> dict)
        {
            accessToken = new AccessToken(SafeDictionary.GetValue<Dictionary<string, object>>(dict, "access_token"));
            openId = SafeDictionary.GetValue<string>(dict, "openid");
            unionId = SafeDictionary.GetValue<string>(dict, "unionid");
            name = SafeDictionary.GetValue<string>(dict, "name");
            avatar = SafeDictionary.GetValue<string>(dict, "avatar");
            email = SafeDictionary.GetValue<string>(dict, "email");
        }

        public TapTapAccount(AccessToken accessToken, string openId, string unionId, string name, string avatar, string email)
        {
            this.accessToken = accessToken;
            this.openId = openId;
            this.unionId = unionId;
            this.name = name;
            this.avatar = avatar;
            this.email = email;
        }

        public string ToJson()
        {
            var dict = new Dictionary<string, object>
            {
                ["access_token"] = accessToken.ToDict(),
                ["openid"] = openId,
                ["unionid"] = unionId,
                ["name"] = name,
                ["avatar"] = avatar,
                ["email"] = email
            };
            return Json.Serialize(dict);
        }

        public TapTapAccount()
        {
            
        }
        
    }
}