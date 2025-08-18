using Newtonsoft.Json;

namespace TapSDK.Login.Internal.Http {
    public class ProfileData {
        [JsonProperty("openid")]
        public string OpenId { get; set; }

        [JsonProperty("unionid")]
        public string UnionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }
        
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
