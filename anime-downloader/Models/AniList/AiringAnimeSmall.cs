using System.Collections.Generic;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class AiringAnimeSmall: ObservableObject
    {
        private string _imagePath;

        [JsonProperty("id")]
        public int Id { get; set; }

        public string ImagePath
        {
            get => _imagePath;
            set => Set(() => ImagePath, ref _imagePath, value);
        }

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
        public IList<string> Synonyms { get; set; }

        [JsonProperty("genres")]
        public IList<string> Genres { get; set; }

        [JsonProperty("adult")]
        public bool Adult { get; set; }

        [JsonProperty("average_score")]
        public double AverageScore { get; set; }

        [JsonProperty("popularity")]
        public int Popularity { get; set; }

        [JsonProperty("updated_at")]
        public int UpdatedAt { get; set; }

        [JsonProperty("hashtag")]
        public string Hashtag { get; set; }

        [JsonProperty("image_url_sml")]
        public string ImageUrlSml { get; set; }

        [JsonProperty("image_url_med")]
        public string ImageUrlMed { get; set; }

        [JsonProperty("image_url_lge")]
        public string ImageUrlLge { get; set; }

        [JsonProperty("image_url_banner")]
        public string ImageUrlBanner { get; set; }

        [JsonProperty("total_episodes")]
        public int TotalEpisodes { get; set; }

        [JsonProperty("airing_status")]
        public string AiringStatus { get; set; }

        [JsonProperty("airing")]
        public Airing Airing { get; set; }

        [JsonProperty("tags")]
        public IList<Tag> Tags { get; set; }
    }
}