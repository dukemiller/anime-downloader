using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class AiringAnime: AiringAnimeSmall
    {
        private string _description;
        private IList<Studio> _studio;
        private string _source;

        [JsonIgnore]
        public AnimeSeason AnimeSeason { get; set; }
        
        [JsonProperty("start_date")]
        public DateTime? StartDate { get; set; }

        [JsonProperty("end_date")]
        public DateTime? EndDate { get; set; }

        [JsonProperty("description")]
        public string Description
        {
            get => _description;
            set => Set(() => Description, ref _description, value);
        }

        [JsonProperty("mean_score")]
        public int MeanScore { get; set; }

        [JsonProperty("favourite")]
        public bool Favourite { get; set; }

        [JsonProperty("youtube_id")]
        public string YoutubeId { get; set; }
        
        [JsonProperty("score_distribution")]
        public object ScoreDistribution { get; set; }

        [JsonProperty("list_stats")]
        public ListStats ListStats { get; set; }
        
        [JsonProperty("duration")]
        public object Duration { get; set; }

        [JsonProperty("source")]
        public string Source
        {
            get => _source;
            set => Set(() => Source, ref _source, value);
        }

        [JsonProperty("classification")]
        public string Classification { get; set; }

        [JsonProperty("airing_stats")]
        public object AiringStats { get; set; }

        [JsonProperty("characters")]
        public IList<Character> Characters { get; set; }

        [JsonProperty("staff")]
        public IList<Staff> Staff { get; set; }

        [JsonProperty("studio")]
        public IList<Studio> Studio
        {
            get => _studio;
            set => Set(() => Studio, ref _studio, value);
        }

        [JsonProperty("external_links")]
        public IList<ExternalLink> ExternalLinks { get; set; }

        [JsonProperty("rankings")]
        public IList<Ranking> Rankings { get; set; }

        [JsonProperty("relations")]
        public IList<Relation> Relations { get; set; }

        [JsonProperty("relations_manga")]
        public IList<object> RelationsManga { get; set; }
        
        [JsonProperty("reviews")]
        public IList<object> Reviews { get; set; }
    }
}