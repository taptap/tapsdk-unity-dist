using System;
using Newtonsoft.Json;
namespace TapSDK.Achievement.Internal.Model
{
    [Serializable]
    public class TapAchievementIncrementRequest
    {
        [JsonProperty("achievement_id")]
        public string AchievementId { get; set; }

        [JsonProperty("steps")]
        public int Steps { get; set; }

        public TapAchievementIncrementRequest(string achievementId, int steps)
        {
            this.AchievementId = achievementId;
            this.Steps = steps;
        }
    }
}