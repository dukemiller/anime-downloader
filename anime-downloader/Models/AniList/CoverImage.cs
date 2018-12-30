using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class CoverImage
    {
        [JsonProperty("large")]
        public string Large { get; set; }
    }
}