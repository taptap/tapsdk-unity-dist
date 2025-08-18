using Newtonsoft.Json;

namespace TapSDK.Compliance.Model 
{
    public class PayableResult 
    {

        [JsonProperty("allow")]
        public bool Status { get; internal set; }

        [JsonProperty("title")]
        public string Title { get; internal set; }

        [JsonProperty("description_plain")]
        public string Content { get; internal set; }
    }

    internal class PayableResponse : BaseResponse 
    {
        [JsonProperty("data")]
        internal PayableResult Result { get; private set; }
    }
}
