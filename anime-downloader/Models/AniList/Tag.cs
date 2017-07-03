using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Tag
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("spoiler")]
        public bool Spoiler { get; set; }

        [JsonProperty("adult")]
        public bool Adult { get; set; }

        [JsonProperty("demographic")]
        public bool Demographic { get; set; }

        [JsonProperty("denied")]
        public int Denied { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("votes")]
        public int Votes { get; set; }

        [JsonProperty("series_spoiler")]
        public bool SeriesSpoiler { get; set; }
    }
}