using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Studios
    {
        [JsonProperty("edges")]
        public IList<StudioEdge> Edges { get; set; }
    }
}