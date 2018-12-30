using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class StartDate
    {
        [JsonProperty("year")]
        public int? Year { get; set; }

        [JsonProperty("month")]
        public int? Month { get; set; }

        [JsonProperty("day")]
        public int? Day { get; set; }
    }
}