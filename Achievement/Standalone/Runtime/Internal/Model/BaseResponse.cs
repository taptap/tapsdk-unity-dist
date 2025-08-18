using Newtonsoft.Json;

namespace TapSDK.Achievement.Internal.Model 
{
    public class BaseResponse 
    {
        [JsonProperty("success")]
        internal bool Success { get; private set; }

         [JsonProperty("now")]
        internal long Now { get; private set; }
    }

}
