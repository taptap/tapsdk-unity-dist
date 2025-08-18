using System;
using Newtonsoft.Json;
namespace TapSDK.Achievement.Internal.Model
{
    [Serializable]
    public class TapAchievementUnlockRequest
    {
        [JsonProperty("achievement_id")]
        public string AchievementId { get; set; }

        public TapAchievementUnlockRequest(string achievementId)
        {
            this.AchievementId = achievementId;
        }
    }
}