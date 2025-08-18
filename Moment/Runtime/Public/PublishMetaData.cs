using System.Collections.Generic;
using Newtonsoft.Json;
namespace TapSDK.Moment
{
    public class PublishMetaData
    {
        // 内容
        [JsonProperty("content")]
        public string Content { get; set; }
        // 图片路径列表
        [JsonProperty("imagePaths")]
        public List<string> ImagePaths { get; set; }

        // 构造函数
        public PublishMetaData(
            string content = "",
            List<string> imagePaths = null)
        {
            Content = content;
            ImagePaths = imagePaths ?? new List<string>();
        }
    }
}