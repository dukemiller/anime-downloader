using System;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using PropertyChanged;

namespace anime_downloader.Models
{
    [Serializable]
    public sealed class AnimeDetails : ObservableObject
    {
        /// <summary>
        ///     The MyAnimeList id number.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        /// <summary>
        ///     The AniList id number.
        /// </summary>
        [JsonProperty("ani_id")]
        public int AniId { get; set; }

        /// <summary>
        ///     The provided synopsis of the plot.
        /// </summary>
        [JsonProperty("synopsis")]
        public string Synopsis { get; set; } = "";

        /// <summary>
        ///     The path to the thumbnail image.
        /// </summary>
        [JsonProperty("image")]
        public string Image { get; set; }

        /// <summary>
        ///     The official title (usually the japanese version).
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; } = "";

        /// <summary>
        ///     The specifically english version of the title.
        /// </summary>
        [JsonProperty("english")]
        public string English { get; set; } = "";

        /// <summary>
        ///     All various nicknames and abbreviations used to represent the show.
        /// </summary>
        [JsonProperty("synonyms")]
        public string Synonyms { get; set; } = "";

        /// <summary>
        ///     True if any change was made that would be pushed to a synchronization service.
        /// </summary>
        [JsonProperty("needs_updates")]
        public bool NeedsUpdating { get; set; }

        /// <summary>
        ///     If the anime was added and no episodes were attempted to be downloaded.
        /// </summary>
        [JsonProperty("just_added")]
        public bool JustAdded { get; set; }

        /// <summary>
        ///     The total episode count for the series.
        /// </summary>
        [JsonProperty("total_episodes")]
        public int TotalEpisodes { get; set; }

        /// <summary>
        ///     The total episodes for the entire series (a sum of prequel series totals to get current episode).
        /// </summary>
        [JsonProperty("overall_total")]
        public int OverallTotal { get; set; }
        
        /// <summary>
        ///     The season that the anime aired on.
        /// </summary>
        [JsonProperty("aired")]
        public AnimeSeason Aired { get; set; }

        /// <summary>
        ///     The season that the anime ended on.
        /// </summary>
        [JsonProperty("ended")]
        public AnimeSeason Ended { get; set; }

        /// <summary>
        ///     The common, publicly used title for the series.
        /// </summary>
        [JsonProperty("preferred_search_title")]
        public string PreferredSearchTitle { get; set; }

        // 

        /// <summary>
        ///     True if the series is still considered airing relative to the current season.
        /// </summary>
        [JsonIgnore]
        public bool AiringNow =>
            Ended > AnimeSeason.Current  ||
            Ended == AnimeSeason.Current && OverallTotal > 13 ||     // end date is in the future AND it has more than a season of episodes
            Aired == AnimeSeason.Current ||                          // season its airing is now, very straightforward
            Aired < AnimeSeason.Current && Ended is null && (        // >> heuristic guessing for missing information
                OverallTotal == 0 ||                                 // unknown ending date (ongoing)
                Total > AnimeSeason.Current.Difference(Aired) * 13)  // enough episodes from airing date
        ;

        /// <summary>
        ///     The calculated total from choosing either the overalltotal or totalepisodes.
        /// </summary>
        [JsonIgnore]
        [DependsOn(nameof(OverallTotal), nameof(TotalEpisodes))]
        public int Total => OverallTotal > 0 ? OverallTotal : TotalEpisodes;

        /// <summary>
        ///     True if the series has a myanimelist id.
        /// </summary>
        [JsonIgnore]
        [DependsOn(nameof(Id))]
        public bool HasId => !string.IsNullOrEmpty(Id);
    }
}