using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Relation
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title_romaji")]
        public string TitleRomaji { get; set; }

        [JsonProperty("title_english")]
        public string TitleEnglish { get; set; }

        [JsonProperty("title_japanese")]
        public string TitleJapanese { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("start_date_fuzzy")]
        public int? StartDateFuzzy { get; set; }

        [JsonProperty("end_date_fuzzy")]
        public int? EndDateFuzzy { get; set; }

        [JsonProperty("season")]
        public int? Season { get; set; }

        [JsonProperty("series_type")]
        public string SeriesType { get; set; }

        [JsonProperty("synonyms")]
        public IList<object> Synonyms { get; set; }

        [JsonProperty("genres")]
        public IList<string> Genres { get; set; }

        [JsonProperty("adult")]
        public bool Adult { get; set; }

        [JsonProperty("average_score")]
        public double? AverageScore { get; set; }

        [JsonProperty("popularity")]
        public int? Popularity { get; set; }

        [JsonProperty("updated_at")]
        public int? UpdatedAt { get; set; }

        [JsonProperty("hashtag")]
        public string Hashtag { get; set; }

        [JsonProperty("image_url_sml")]
        public string ImageUrlSml { get; set; }

        [JsonProperty("image_url_med")]
        public string ImageUrlMed { get; set; }

        [JsonProperty("image_url_lge")]
        public string ImageUrlLge { get; set; }

        [JsonProperty("image_url_banner")]
        public object ImageUrlBanner { get; set; }

        [JsonProperty("total_episodes")]
        public int? TotalEpisodes { get; set; }

        [JsonProperty("airing_status")]
        public string AiringStatus { get; set; }

        [JsonProperty("relation_type")]
        public string RelationType { get; set; }

        [JsonProperty("link_id")]
        public int LinkId { get; set; }
    }
}