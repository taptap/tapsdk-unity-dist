using Newtonsoft.Json;

namespace TapSDK.Compliance.Model 
{
    public class StandaloneResponse 
    {
        [JsonProperty("stand_alone_mode")]
        private int Mode { get; set; }

        public bool Enabled => Mode == 1;
    }
}
