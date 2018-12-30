using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class RelationEdge
    {
        [JsonProperty("relationType")]
        public string RelationType { get; set; }

        [JsonProperty("node")]
        public RelationNode Node { get; set; }
    }
}