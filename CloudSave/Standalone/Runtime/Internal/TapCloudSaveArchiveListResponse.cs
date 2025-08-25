using System.Collections.Generic;
using Newtonsoft.Json;

namespace TapSDK.CloudSave.Standalone
{
    internal class TapCloudSaveArchiveListResponse
    {
        [JsonProperty("saves")]
        public List<ArchiveData> saves { get; set; }
    }
}
