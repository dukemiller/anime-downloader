using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Page
    {
        [JsonProperty("pageInfo")]
        public PageInfo PageInfo { get; set; }

        [JsonProperty("media")]
        public IList<AiringAnime> Media { get; set; }
    }
}