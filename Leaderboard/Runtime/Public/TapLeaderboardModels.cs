using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.Leaderboard
{
    /// <summary>
    /// 排行榜分数响应
    /// </summary>
    public class LeaderboardScoreResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("leaderboard")]
        public Leaderboard leaderboard { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("scores")]
        public List<Score> scores { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("next_page")]
        public string nextPage { get; set; }
    }

    /// <summary>
    /// 用户分数响应
    /// </summary>
    public class UserScoreResponse
    {
        /// <summary>
        /// 当前用户的分数信息
        /// </summary>
        [JsonProperty("current_user_score")]
        public Score currentUserScore { get; set; }

        /// <summary>
        /// 排行榜信息
        /// </summary>
        [JsonProperty("leaderboard")]
        public Leaderboard leaderboard { get; set; }
    }

    /// <summary>
    /// 提交分数请求类
    /// 一次最多可以提交5个分数
    /// </summary>
    public class SubmitScoresRequest
    {
        /// <summary>
        /// 分数列表，最多5个
        /// </summary>
        [JsonProperty("submit_scores")]
        public List<ScoreItem> scores { get; set; }

        /// <summary>
        /// 单个分数项
        /// </summary>
        public class ScoreItem
        {
            /// <summary>
            /// 排行榜ID
            /// </summary>
            [JsonProperty("leaderboard_id")] public string LeaderboardId { get; set; }

            /// <summary>
            /// 分数值
            /// </summary>
            [JsonProperty("score")] public long Score { get; set; }
        }
    }

    /// <summary>
    /// 提交分数响应
    /// </summary>
    public class SubmitScoresResponse
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("results")]
        public List<Item> items { get; set; }
        
        /// <summary>
        /// 提交分数响应项
        /// </summary>
        public class Item
        {
            /// <summary>
            /// 
            /// </summary>
            [JsonProperty("leaderboard_id")]
            public string leaderboardId { get; set; }
            
            /// <summary>
            /// 用户 ID
            /// </summary>
            [JsonProperty("openid")]
            public string openId { get; set; }

            /// <summary>
            /// 周期 Token
            /// </summary>
            [JsonProperty("period_token")]
            public string periodToken { get; set; }

            /// <summary>
            /// 分数结果
            /// </summary>
            [JsonProperty("score_result")]
            public Score score { get; set; }

            /// <summary>
            /// 用户 ID
            /// </summary>
            [JsonProperty("unionid")]
            public string unionId { get; set; }

            /// <summary>
            /// 分数结果类
            /// </summary>
            public class Score
            {
                /// <summary>
                /// 
                /// </summary>
                [JsonProperty("new_best")]
                public bool newBest { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [JsonProperty("raw_score")]
                public long rawScore { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [JsonProperty("score_display")]
                public string scoreDisplay { get; set; }
            }
        }
    }
}