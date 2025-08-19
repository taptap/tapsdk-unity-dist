using Newtonsoft.Json;

namespace TapSDK.Login.Internal.Http {
    public class QRCodeData {
        [JsonProperty("device_code")]
        public string DeviceCode { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("qrcode_url")]
        public string Url { get; set; }

        [JsonProperty("interval")]
        public int Interval { get; set; }
    }
}