using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TapSDK.Core.Standalone.Internal.Http
{

    [Serializable]
    public class TapHttpResponse
    {
        [JsonProperty("data")]
        public JObject Data { get; private set; }

        [JsonProperty("success")]
        public bool Success { get; private set; }

        [JsonProperty("now")]
        public int Now { get; private set; }
    }

    [Serializable]
    public class TapHttpErrorData
    {
        [JsonProperty("code")]
        public int Code { get; private set; }

        [JsonProperty("msg")]
        public string Msg { get; private set; }

        [JsonProperty("error")]
        public string Error { get; private set; }

        [JsonProperty("error_description")]
        public string ErrorDescription { get; private set; }
    }

}