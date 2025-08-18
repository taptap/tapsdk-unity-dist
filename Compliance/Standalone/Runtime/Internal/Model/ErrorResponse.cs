using Newtonsoft.Json;

namespace TapSDK.Compliance.Model 
{
    internal class ErrorResult 
    {
        [JsonProperty("error")]
        internal string Error { get; private set; }

        [JsonProperty("error_description")]
        internal string Description { get; private set; }

        [JsonProperty("msg")]
        internal string Message { get; private set; }

        [JsonProperty("code")]
        internal long ErrorCode { get; private set; }


    }

    internal class ErrorResponse : BaseResponse 
    {
        [JsonProperty("data")]
        internal ErrorResult Result { get; private set; }
    }
}
