using Newtonsoft.Json;
using TapSDK.Compliance.Model;

namespace TapSDK.Compliance 
{
    public class PlayableResult 
    {
        private static readonly int MAX_CHINA_REMAIN_TIME = 60;
        
        /// <summary>
        /// 单日游戏最大剩余时间(分钟)
        /// </summary>
        internal static int MaxRemainTime
        {
            get
            {
                return MAX_CHINA_REMAIN_TIME;
            }
        }

        /// <summary>
        /// 剩余时长，用于 UI 展示
        /// </summary>
        [JsonProperty("remain_time")]
        public int RemainTime { get; set; }
       

        /// <summary>
        /// 标题，用于 UI 展示
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 内容，用于 UI 展示
        /// </summary>
        [JsonProperty("description_plain")]
        public string Content { get; set; }


    }

    internal class PlayableResponse : BaseResponse 
    {
        [JsonProperty("data")]
        public PlayableResult Result;
    }
}