using System;
using Newtonsoft.Json;

namespace TapSDK.RelationLite
{
    [Serializable]
    public class RelationLiteUserItem
    {
        [JsonProperty("follow_status")]
        public FollowStatus followStatus;

        [JsonProperty("created_at")]
        public long createdAt;

        [JsonProperty("user")]
        public RelationLiteUserInfo user;
    }
} 