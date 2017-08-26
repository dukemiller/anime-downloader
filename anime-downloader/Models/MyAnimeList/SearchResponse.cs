using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.MyAnimeList
{
    public class SearchResponse
    {
        [JsonProperty("categories")]
        public IList<Category> Categories { get; set; }
    }
}