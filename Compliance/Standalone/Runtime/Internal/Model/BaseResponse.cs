using Newtonsoft.Json;

namespace TapSDK.Compliance.Model 
{
    internal class BaseResponse 
    {
        [JsonProperty("success")]
        internal bool Success { get; private set; }

         [JsonProperty("now")]
        internal long Now { get; private set; }
    }

}
