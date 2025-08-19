using Newtonsoft.Json;

namespace TapSDK.Core
{
    public class TapEngineBridgeResult
    {
        public const int RESULT_SUCCESS = 0;
        public const int RESULT_ERROR = -1;

        [JsonProperty("code")] public int code { get; set; }
        [JsonProperty("message")] public string message { get; set; }
        [JsonProperty("content")] public string content { get; set; }
    }
}