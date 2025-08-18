using System;
using Newtonsoft.Json;

namespace TapSDK.RelationLite
{
    public class RelationLiteUserInfo
    {
        [JsonProperty("alias")]
        public string alias;

        [JsonProperty("avatar")]
        public string avatar;

        [JsonProperty("name")]
        public string name;

        [JsonProperty("open_id")]
        public string openId;
    }
} 