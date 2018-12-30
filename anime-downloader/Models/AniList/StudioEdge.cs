using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class StudioEdge
    {
        [JsonProperty("isMain")]
        public bool IsMain { get; set; }

        [JsonProperty("node")]
        public StudioNode StudioNode { get; set; }
    }
}