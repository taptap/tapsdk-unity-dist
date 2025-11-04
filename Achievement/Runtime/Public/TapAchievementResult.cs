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
            // 修复：添加null/空字符串检查，防止反序列化崩溃
            if (string.IsNullOrEmpty(json))
            {
                throw new System.ArgumentException("Cannot deserialize null or empty JSON string to TapAchievementResult");
            }
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