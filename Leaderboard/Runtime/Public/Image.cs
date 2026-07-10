using Newtonsoft.Json;

namespace TapSDK.Leaderboard
{
    /// <summary>
    /// 排行榜背景
    /// NOTE: This class was not defined in the provided Kotlin snippet.
    /// It is an assumption based on the 'background: Image?' property.
    /// </summary>
    public class Image
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("color")]
        public string color { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("gif_url")]
        public string gifUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("height")]
        public int? height { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("width")]
        public int? width { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("medium_url")]
        public string mediumUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("original_format")]
        public string originalFormat { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("original_size")]
        public int? originalSize { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("original_url")]
        public string originalUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("small_url")]
        public string smallUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("url")]
        public string url { get; set; }
    }
}