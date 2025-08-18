using Newtonsoft.Json;

namespace TapSDK.Achievement
{
    [System.Serializable]
    public class TapAchievementResult
    {
        [JsonProperty("achievementId")]
        public string AchievementId { get; set; }

        [JsonProperty("achievementName")]
        public string AchievementName { get; set; }

        [JsonProperty("achievementType")]
        public TapAchievementType AchievementType { get; set; }

        [JsonProperty("currentSteps")]
        public long CurrentSteps { get; set; }

        public TapAchievementResult(string achievementId, string achievementName, TapAchievementType achievementType = TapAchievementType.NORMAL, long currentSteps = 0)
        {
            AchievementId = achievementId;
            AchievementName = achievementName;
            AchievementType = achievementType;
            CurrentSteps = currentSteps;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static TapAchievementResult FromJson(string json)
        {
            return JsonConvert.DeserializeObject<TapAchievementResult>(json);
        }
    }

    public enum TapAchievementType
    {
        // 普通成就
        NORMAL,
        // 白金成就
        PLATINUM
    }
}