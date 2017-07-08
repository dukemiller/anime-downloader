using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class ExternalLink
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("site")]
        public string Site { get; set; }
    }
}