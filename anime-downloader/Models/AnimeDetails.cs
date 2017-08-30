using System;
using System.Collections.Generic;
using anime_downloader.Enums;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models
{
    [Serializable]
    public sealed class AnimeDetails : ObservableObject
    {
        private static int CurrentYear() => DateTime.Now.Year;

        private static Season CurrentSeason() => (Season) Math.Ceiling(Convert.ToDouble(DateTime.Now.Month) / 3);

        private string _english = "";

        private string _id = "";

        private int _aniId = 0;

        private bool _needsUpdating;

        private int _overallTotal;

        private string _synonyms = "";

        private string _synopsis = "";

        private string _title = "";

        private int _totalEpisodes;

        [JsonProperty("id")]
        public string Id
        {
            get => _id;
            set
            {
                Set(() => Id, ref _id, value);
                RaisePropertyChanged(nameof(HasId));
            }
        }

        [JsonProperty("ani_id")]
        public int AniId
        {
            get => _aniId;
            set => Set(() => AniId, ref _aniId, value);
        }

        [JsonProperty("synopsis")]
        public string Synopsis
        {
            get => _synopsis;
            set => Set(() => Synopsis, ref _synopsis, value);
        }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("title")]
        public string Title
        {
            get => _title;
            set
            {
                Set(() => Title, ref _title, value);
                RaisePropertyChanged(nameof(TitleAndEnglish));
            }
        }

        [JsonProperty("english")]
        public string English
        {
            get => _english;
            set
            {
                Set(() => English, ref _english, value);
                RaisePropertyChanged(nameof(TitleAndEnglish));
            }
        }

        [JsonProperty("synonyms")]
        public string Synonyms
        {
            get => _synonyms;
            set
            {
                Set(() => Synonyms, ref _synonyms, value);
                RaisePropertyChanged(nameof(SynonymsSplit));
            }
        }

        [JsonProperty("needs_updates")]
        public bool NeedsUpdating
        {
            get => _needsUpdating;
            set => Set(() => NeedsUpdating, ref _needsUpdating, value);
        }

        [JsonProperty("total_episodes")]
        public int TotalEpisodes
        {
            get => _totalEpisodes;
            set
            {
                Set(() => TotalEpisodes, ref _totalEpisodes, value);
                RaisePropertyChanged(nameof(Total));
            }
        }

        [JsonProperty("overall_total")]
        public int OverallTotal
        {
            get => _overallTotal;
            set
            {
                Set(() => OverallTotal, ref _overallTotal, value);
                RaisePropertyChanged(nameof(Total));
            }
        }

        [JsonProperty("series_continuation_episode")]
        public string SeriesContinuationEpisode { get; set; }
        
        [JsonProperty("aired")]
        public AnimeSeason Aired { get; set; }

        [JsonProperty("ended")]
        public AnimeSeason Ended { get; set; }

        [JsonProperty("preferred_search_title")]
        public string PreferredSearchTitle { get; set; }

        // 

        [JsonIgnore]
        public bool AiringNow
        {
            /*
             * Given that the airing is not null and that either the year is less than the current year
             * or that the year is the same as the current year and the season is less than or equal to the current season,
             * combined with the fact that there is either no end date or the end date is this season, then the show
             * has to be airing
             * 
             * Starting airing in 2007,
             *    No End Date
             *    Current is 2016
             *    = Still airing e.g. Naruto Shippuden
             * Started airing in Summer 2016, 
             *     Ended Airing in Fall 2017
             *     Current is Fall 2017
             *     = Still airing e.g. Twin Star Exorcists
             * */
            get
            {
                return Aired != null
                       && (Aired.Year < CurrentYear() || (Aired.Year == CurrentYear() && (int) Aired.Season <= (int) CurrentSeason()))
                       && (Ended == null || (Ended.Year == CurrentYear() && (int) Ended.Season == (int) CurrentSeason()));
            }
        }

        [JsonIgnore]
        public int Total => OverallTotal > 0 ? OverallTotal : TotalEpisodes;

        [JsonIgnore]
        public bool HasId => !string.IsNullOrEmpty(Id);

        [JsonIgnore]
        public IEnumerable<string> TitleAndEnglish => new[] {Title, English};

        [JsonIgnore]
        public IEnumerable<string> SynonymsSplit => Synonyms.Split(';');
    }
}