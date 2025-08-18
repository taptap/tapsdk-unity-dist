using System;
using Newtonsoft.Json;

namespace TapSDK.RelationLite
{
    public class FollowStatus
    {
        [JsonProperty("blocked_by")]
        public bool blockedBy;

        [JsonProperty("blocking")]
        public bool blocking;

        [JsonProperty("followed_by")]
        public bool followedBy;

        [JsonProperty("following")]
        public bool following;
    }
} 