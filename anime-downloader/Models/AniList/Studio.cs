using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Studio
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("studio_name")]
        public string StudioName { get; set; }

        [JsonProperty("studio_wiki")]
        public object StudioWiki { get; set; }

        [JsonProperty("favourite")]
        public bool Favourite { get; set; }

        [JsonProperty("link_id")]
        public int LinkId { get; set; }

        [JsonProperty("main_studio")]
        public int MainStudio { get; set; }
    }
}