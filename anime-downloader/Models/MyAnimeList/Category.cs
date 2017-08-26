using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.MyAnimeList
{
    public class Category
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("items")]
        public IList<Item> Items { get; set; }
    }
}