using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.Moment
{
    public class DisplayInfo
    {
        // 页面名称
        [JsonProperty("page")]
        public string Page { get; set; }
        // 额外信息
        [JsonProperty("extras")]
        public Dictionary<string, string> Extras { get; set; }

        // 构造函数
        public DisplayInfo(string page = "", Dictionary<string, string> extras = null)
        {
            Page = page;
            Extras = extras ?? new Dictionary<string, string>();
        }
    }
}