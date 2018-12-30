using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class PageInfo
    {
        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("hasNextPage")]
        public bool HasNextPage { get; set; }
    }
}