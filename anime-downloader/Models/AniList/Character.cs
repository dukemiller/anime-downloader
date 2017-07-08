using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Character
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name_first")]
        public string NameFirst { get; set; }

        [JsonProperty("name_last")]
        public string NameLast { get; set; }

        [JsonProperty("image_url_lge")]
        public string ImageUrlLge { get; set; }

        [JsonProperty("image_url_med")]
        public string ImageUrlMed { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("link_id")]
        public int LinkId { get; set; }

        [JsonProperty("actor")]
        public IList<Actor> Actor { get; set; }
    }
}