using System;
using Newtonsoft.Json;

namespace TapSDK.Achievement.Internal.Model
{
    [Serializable]
    public class TapAchievementResponseData
    {
        [JsonProperty("achievement")]
        public TapAchievementResponseBean Achievement { get; set; }

        [JsonProperty("platinum_achievement")]
        public TapAchievementResponseBean PlatinumAchievement { get; set; }

        // Constructor
        public TapAchievementResponseData(TapAchievementResponseBean achievement, TapAchievementResponseBean platinumAchievement)
        {
            this.Achievement = achievement;
            this.PlatinumAchievement = platinumAchievement;
        }
    }

    [Serializable]
    public class TapAchievementResponseBean
    {
        [JsonProperty("current_steps")]
        public int CurrentSteps { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("newly_unlocked")]
        public bool NewlyUnlocked { get; set; }
    }
}
