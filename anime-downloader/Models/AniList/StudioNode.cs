using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class StudioNode
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}