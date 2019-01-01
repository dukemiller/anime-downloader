using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Title
    {
        [JsonProperty("romaji")]
        public string Romaji { get; set; } = "";

        [JsonProperty("english")]
        public string English { get; set; } = "";

        [JsonIgnore]
        public string Main => !string.IsNullOrEmpty(English) ? English : Romaji;
    }
}