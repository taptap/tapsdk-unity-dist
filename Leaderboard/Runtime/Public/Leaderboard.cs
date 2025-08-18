using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.Leaderboard
{
    /// <summary>
    /// 排行榜信息
    /// </summary>
    public class Leaderboard
    {
        /// <summary>
        /// 可用的时间周期
        /// </summary>
        [JsonProperty("available_periods")]
        public List<Period> availablePeriods { get; set; }

        /// <summary>
        /// 排行榜背景
        /// </summary>
        [JsonProperty("background")]
        public Image background { get; set; }

        /// <summary>
        /// 排行榜ID
        /// </summary>
        [JsonProperty("id")]
        public string id { get; set; }

        /// <summary>
        /// 排行榜名称
        /// </summary>
        [JsonProperty("name")]
        public string name { get; set; }

        /// <summary>
        /// 当前周期
        /// </summary>
        [JsonProperty("period")]
        public Period period { get; set; }

        /// <summary>
        /// 排行榜分数
        /// NOTE: The type 'Score' was not defined in the provided Kotlin snippet.
        /// Assuming it corresponds to the existing 'UserScore' class.
        /// </summary>
        [JsonProperty("score")]
        public Score score { get; set; }
    }

    /// <summary>
    /// 时间周期
    /// </summary>
    public class Period
    {
        /// <summary>
        /// 展示文本
        /// </summary>
        [JsonProperty("display")]
        public string display { get; set; }

        /// <summary>
        /// 周期标识
        /// </summary>
        [JsonProperty("period_token")]
        public string periodToken { get; set; }
    }
}