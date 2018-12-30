using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class AniListResponse
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
    }
}
