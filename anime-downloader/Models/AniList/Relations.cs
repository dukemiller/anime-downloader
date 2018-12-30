using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Relations
    {
        [JsonProperty("edges")]
        public IList<RelationEdge> Edges { get; set; }
    }
}