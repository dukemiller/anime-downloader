using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class RelationNode
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("episodes")]
        public int? Episodes { get; set; }
    }
}