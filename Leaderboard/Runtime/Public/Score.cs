using System;
using Newtonsoft.Json;

namespace TapSDK.Leaderboard
{
    /// <summary>
    /// 当前用户的分数信息
    /// </summary>
    public class Score
    {
        /// <summary>
        /// 用户排名
        /// </summary>
        [JsonProperty("rank")]
        public long? rank { get; set; }

        /// <summary>
        /// 排名展示文本
        /// </summary>
        [JsonProperty("rank_display")]
        public string rankDisplay { get; set; }

        /// <summary>
        /// 用户分数
        /// </summary>
        [JsonProperty("score")]
        public long? score { get; set; }

        /// <summary>
        /// 分数展示文本
        /// </summary>
        [JsonProperty("score_display")]
        public string scoreDisplay { get; set; }

        /// <summary>
        /// 分数提交时间
        /// </summary>
        [JsonProperty("score_submitted_time")]
        public long? scoreSubmittedTime { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        [JsonProperty("user")]
        public User user { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        [Serializable]
        public class User
        {
            /// <summary>
            /// 用户头像
            /// </summary>
            [JsonProperty("avatar")]
            public Image avatar { get; set; }

            /// <summary>
            /// 用户名称
            /// </summary>
            [JsonProperty("name")]
            public string name { get; set; }

            /// <summary>
            /// 开放平台ID
            /// </summary>
            [JsonProperty("openid")]
            public string openid { get; set; }

            /// <summary>
            /// 统一ID
            /// </summary>
            [JsonProperty("unionid")]
            public string unionid { get; set; }
        }
    }
}