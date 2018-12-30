using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Data
    {
        [JsonProperty("Page")]
        public Page Page { get; set; }
    }
}