using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Ranking
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("type_string")]
        public string TypeString { get; set; }

        [JsonProperty("ranking_type")]
        public string RankingType { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("year")]
        public int? Year { get; set; }

        [JsonProperty("season")]
        public string Season { get; set; }
    }
}