using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class AiringAnime
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("idMal")]
        public int? IdMal { get; set; }

        [JsonProperty("title")]
        public Title Title { get; set; }

        [JsonProperty("genres")]
        public IList<string> Genres { get; set; }

        [JsonProperty("startDate")]
        public StartDate StartDate { get; set; }

        [JsonProperty("endDate")]
        public EndDate EndDate { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("relations")]
        public Relations Relations { get; set; }

        [JsonProperty("episodes")]
        public int? Episodes { get; set; }

        [JsonProperty("coverImage")]
        public CoverImage CoverImage { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("studios")]
        public Studios Studios { get; set; }
    }
}