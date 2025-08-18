using System;
using Newtonsoft.Json;
namespace TapSDK.Achievement.Internal.Model
{
    [Serializable]
    public class TapAchievementStoreBean
    {
        [JsonProperty("achievement_id")]
        public string AchievementId { get; set; }

        [JsonProperty("steps")]
        public int Steps { get; set; }

        // 0 - increment, 1 - unlock
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("timeMilliseconds")]
        public long TimeMilliseconds { get; set; }

        [JsonProperty("uuid")]
        public string UUID { get; set; }

        public TapAchievementStoreBean(int type, string achievementId, int steps = 0)
        {
            this.AchievementId = achievementId;
            this.Steps = steps;
            this.Type = type;
            this.TimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.UUID = Guid.NewGuid().ToString();
        }
    }
}