using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.Core.Standalone.Internal.Bean
{
    [Serializable]
    internal class TapGatekeeper
    {
        [JsonProperty("switch")]
        public TapGatekeeperSwitch Switch { get; set; } = new TapGatekeeperSwitch();

        [JsonProperty("urls")]
        public Dictionary<string, Url> Urls { get; set; }

        [JsonProperty("taptap_app_id")]
        public int? TapTapAppId { get; set; }
    }

    [Serializable]
    internal class TapGatekeeperSwitch
    {

        [JsonProperty("heartbeat")]
        public bool Heartbeat { get; set; } = true;
    }

    [Serializable]
    internal class Url
    {
        [JsonProperty("webview")]
        public string WebView { get; set; }

        [JsonProperty("browser")]
        public string Browser { get; set; }

        [JsonProperty("uri")]
        public string TapUri { get; set; }
    }
}
