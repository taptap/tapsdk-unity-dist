using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TapSDK.CloudSave.Standalone
{
    internal class TapCloudSaveBaseResponse
    {
        [JsonProperty("success")]
        public bool success { get; set; }

        [JsonProperty("now")]
        public int now { get; set; }

        [JsonProperty("data")]
        public JObject data { get; set; }
    }

    internal class TapCloudSaveError
    {
        [JsonProperty("code")]
        public int code { get; set; }

        [JsonProperty("msg")]
        public string msg { get; set; }

        [JsonProperty("error")]
        public string error { get; set; }
    }
}
