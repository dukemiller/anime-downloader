using System;
using anime_downloader.Enums;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models
{
    [Serializable]
    public class Anime : ObservableObject
    {
        /// <summary>
        ///     Main referenced title.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        /// <summary>
        ///     User's current watched episode.
        /// </summary>
        [JsonProperty("episode")]
        public int Episode { get; set; }

        /// <summary>
        ///     User's status on watching the anime.
        /// </summary>
        [JsonProperty("status")]
        public Status Status { get; set; } = Status.Watching;

        /// <summary>
        ///     The quality to be downloaded.
        /// </summary>
        [JsonProperty("resolution")]
        public string Resolution { get; set; } = "720";

        /// <summary>
        ///     If the anime is ongoing and currently airing.
        /// </summary>
        [JsonProperty("airing")]
        public bool Airing { get; set; } = true;

        /// <summary>
        ///     if searching for the anime should contain exclusively it's own name with no fragments.
        /// </summary>
        [JsonProperty("name_strict")]
        public bool NameStrict { get; set; }

        /// <summary>
        ///     If searching for the anime should only download from a specific subgroup if chosen
        /// </summary>
        [JsonProperty("preferred_subgroup")]
        public string PreferredSubgroup { get; set; }

        /// <summary>
        ///     The personal rating given for the series.
        /// </summary>
        [JsonProperty("rating")]
        public string Rating { get; set; } = "";

        [JsonProperty("details")]
        public AnimeDetails Details { get; set; } = new AnimeDetails();

        /// <summary>
        ///     User written notes about the show.
        /// </summary>
        [JsonProperty("notes")]
        public string Notes { get; set; } = "";

        // 

        /// <summary>
        ///     Proper title name of anime.
        /// </summary>
        [JsonIgnore]
        public string Title => App.TextInfo.ToTitleCase(Name);

        /// <summary>
        ///     A zero padded string of the number of the next episode.
        /// </summary>
        [JsonIgnore]
        public int NextEpisode => Episode + 1;

        /// <summary>
        ///     The difference between the overall total and the current episode
        /// </summary>
        [JsonIgnore]
        public int SeriesContinuationEpisode => Details.TotalEpisodes - (Details.OverallTotal - Episode);

        /// <summary>
        ///     A string representation display of the episode total, e.g. {current}/{total}
        /// </summary>
        [JsonIgnore]
        public string EpisodeTotal
        {
            get
            {
                // If there's data about the episode number, retrieve it
                if (Details.TotalEpisodes > 0 || Details.HasId)
                {
                    // If it has an overall total, this was a mislabeled show and this
                    // needs to be preferred first
                    if (Details.OverallTotal > 0)
                        return $"{Episode}/{Details.OverallTotal}";

                    // Else just the actual season total if there is one
                    if (Details.TotalEpisodes > 0)
                        return $"{Episode}/{Details.TotalEpisodes}";
                }

                return Episode.ToString();
            }
        }
    }
}