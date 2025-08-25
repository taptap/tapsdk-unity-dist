using Newtonsoft.Json;

namespace TapSDK.CloudSave
{
    public class ArchiveMetadata
    {
        public ArchiveMetadata(string archiveName, string archiveSummary, string archiveExtra, int archivePlaytime)
        {
            Name = archiveName;
            Summary = archiveSummary;
            Extra = archiveExtra;
            Playtime = archivePlaytime;
        }

        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("summary")] public string Summary { get; set; }
        [JsonProperty("extra")] public string Extra { get; set; }
        [JsonProperty("playtime")] public int Playtime { get; set; }
    }

    public class ArchiveData
    {
        [JsonProperty("uuid")] public string Uuid { get; set; }
        [JsonProperty("file_id")] public string FileId { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("summary")] public string Summary { get; set; }
        [JsonProperty("extra")] public string Extra { get; set; }
        [JsonProperty("playtime")] public int Playtime { get; set; }
        [JsonProperty("save_size")] public long ArchiveFileSize { get; set; }
        [JsonProperty("cover_size")] public long ArchiveCoverSize { get; set; }
        [JsonProperty("created_time")] public long CreatedTime { get; set; }
        [JsonProperty("modified_time")] public long ModifiedTime { get; set; }
    }
}