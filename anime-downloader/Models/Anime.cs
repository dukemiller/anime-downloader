using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using anime_downloader.Enums;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    [Serializable]
    public class Anime : ObservableObject
    {
        /// <summary>
        ///     A variable used sort of like a bit flag for sorting in the data grid.
        /// </summary>
        public static int SortedRateFlag;

        /// <summary>
        ///     Another variable used sort of like a bit flag for sorting in the data grid.
        /// </summary>
        public static int SortedAiredFlag;

        private bool _airing;

        private int _episode;

        private MyAnimeListDetails _myAnimeList;
        private string _name;

        private bool _nameStrict;

        private string _notes;

        private string _preferredSubgroup;

        private string _rating;

        private string _resolution;

        private bool _secondSeason;

        private Status _status;

        // 

        public Anime() => MyAnimeList = new MyAnimeListDetails();

        // 

        /// <summary>
        ///     Main referenced title.
        /// </summary>
        [XmlAttribute("name")]
        public string Name
        {
            get => _name;
            set => Set(() => Name, ref _name, value);
        }

        /// <summary>
        ///     User's current watched episode.
        /// </summary>
        [XmlAttribute("episode")]
        public int Episode
        {
            get => _episode;
            set
            {
                Set(() => Episode, ref _episode, value);
                if (MyAnimeList.HasId)
                    MyAnimeList.NeedsUpdating = true;
            }
        }

        /// <summary>
        ///     User's status on watching the anime.
        /// </summary>
        [XmlAttribute("status")]
        public Status Status
        {
            get => _status;
            set
            {
                Set(() => Status, ref _status, value);
                if (MyAnimeList.HasId)
                    MyAnimeList.NeedsUpdating = true;
            }
        }

        /// <summary>
        ///     The quality to be downloaded.
        /// </summary>
        [XmlAttribute("resolution")]
        public string Resolution
        {
            get => _resolution;
            set => Set(() => Resolution, ref _resolution, value);
        }

        /// <summary>
        ///     If the anime is ongoing and currently airing.
        /// </summary>
        [XmlAttribute("airing")]
        public bool Airing
        {
            get => _airing;
            set => Set(() => Airing, ref _airing, value);
        }

        /// <summary>
        ///     if searching for the anime should contain exclusively it's own name with no fragments.
        /// </summary>
        [XmlAttribute("name_strict")]
        public bool NameStrict
        {
            get => _nameStrict;
            set => Set(() => NameStrict, ref _nameStrict, value);
        }

        /// <summary>
        ///     If searching for the anime should only download from a specific subgroup if chosen
        /// </summary>
        [XmlAttribute("preferred_subgroup")]
        public string PreferredSubgroup
        {
            get => _preferredSubgroup;
            set => Set(() => PreferredSubgroup, ref _preferredSubgroup, value);
        }

        /// <summary>
        ///     The personal rating given for the series.
        /// </summary>
        [XmlAttribute("rating")]
        public string Rating
        {
            get => _rating;
            set => Set(() => Rating, ref _rating, value);
        }

        [XmlElement("my_anime_list")]
        public MyAnimeListDetails MyAnimeList
        {
            get => _myAnimeList;
            set => Set(() => MyAnimeList, ref _myAnimeList, value);
        }

        [XmlAttribute("notes")]
        public string Notes
        {
            get => _notes;
            set
            {
                Set(() => Notes, ref _notes, value);
                if (MyAnimeList.HasId)
                    MyAnimeList.NeedsUpdating = true;
            }
        }

        [XmlAttribute("is_second_season")]
        public bool SecondSeason
        {
            get => _secondSeason;
            set => Set(() => SecondSeason, ref _secondSeason, value);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        /* Getters */

        /// <summary>
        ///     Proper title name of anime.
        /// </summary>
        /// <returns>A title</returns>
        [XmlIgnore]
        public string Title => new CultureInfo("en-US", false).TextInfo.ToTitleCase(Name);

        /// <summary>
        ///     A zero padded string of the number of the next episode.
        /// </summary>
        /// <returns>A padded string representation of the next episode in sequence.</returns>
        [XmlIgnore]
        public int NextEpisode => Episode + 1;

        [XmlIgnore]
        public string AiringSymbol => Airing ? "✓" : "";

        [XmlIgnore]
        public bool HasRating => Rating != null;

        [XmlIgnore]
        public string EpisodeTotal
        {
            get
            {
                // If there's data about the episode number, retrieve it
                if (MyAnimeList.HasId)
                {
                    // If it has an overall total, this was a mislabeled show and this
                    // needs to be preferred first
                    if (MyAnimeList.OverallTotal > 0)
                        return $"{Episode}/{MyAnimeList.OverallTotal}";

                    // Else just the actual season total if there is one
                    if (MyAnimeList.TotalEpisodes > 0)
                        return $"{Episode}/{MyAnimeList.TotalEpisodes}";
                }

                return Episode.ToString();
            }
        }

        /// <summary>
        ///     A property used for sorting the rating in the datagrid
        /// </summary>
        [XmlIgnore]
        public int SortedRating => string.IsNullOrEmpty(Rating) ? 13 * SortedRateFlag - 2 : int.Parse(Rating);

        [XmlIgnore]
        public int SeasonSort => MyAnimeList.Aired?.Sort ?? (DateTime.Now.Year + 3) * SortedAiredFlag - 2;

        [XmlIgnore]
        public IEnumerable<string> NameCollection
        {
            get
            {
                IEnumerable<string> names;

                if (MyAnimeList.HasId)
                    names = new[] {MyAnimeList.English, MyAnimeList.Title}
                        .Union(MyAnimeList.SynonymsSplit)
                        .SelectMany(c => c.Split())
                        .Distinct();
                else
                    names = Title.Split().Distinct();

                return names.Where(c => c.Length > 0);
            }
        }
    }
}