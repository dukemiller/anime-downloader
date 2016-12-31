using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models.MyAnimeList;
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

        private string _name;
        private Status _status;
        private string _resolution;
        private bool _airing;
        private bool _nameStrict;
        private string _preferredSubgroup;
        private string _notes;
        private string _rating;
        private int _episode;
        private MyAnimeListDetails _myAnimeList;

        // 
        
        public Anime()
        {
            MyAnimeList = new MyAnimeListDetails();
        }

        // 

        /// <summary>
        ///     Main referenced title.
        /// </summary>
        [XmlAttribute("name")]
        public string Name
        {
            get { return _name; }
            set { Set(() => Name, ref _name, value); }
        }

        /// <summary>
        ///     User's current watched episode.
        /// </summary>
        [XmlAttribute("episode")]
        public int Episode
        {
            get { return _episode; }
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
            get { return _status; }
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
            get { return _resolution; }
            set { Set(() => Resolution, ref _resolution, value); }
        }

        /// <summary>
        ///     If the anime is ongoing and currently airing.
        /// </summary>
        [XmlAttribute("airing")]
        public bool Airing
        {
            get { return _airing; }
            set { Set(() => Airing, ref _airing, value); }
        }

        /// <summary>
        ///     if searching for the anime should contain exclusively it's own name with no fragments.
        /// </summary>
        [XmlAttribute("name_strict")]
        public bool NameStrict
        {
            get { return _nameStrict; }
            set { Set(() => NameStrict, ref _nameStrict, value); }
        }

        /// <summary>
        ///     If searching for the anime should only download from a specific subgroup if chosen
        /// </summary>
        [XmlAttribute("preferred_subgroup")]
        public string PreferredSubgroup
        {
            get { return _preferredSubgroup; }
            set { Set(() => PreferredSubgroup, ref _preferredSubgroup, value); }
        }

        /// <summary>
        ///     The personal rating given for the series.
        /// </summary>
        [XmlAttribute("rating")]
        public string Rating
        {
            get { return _rating; }
            set
            {
                if (value != null)
                {
                    var number = int.Parse(value);
                    number = Math.Min(Math.Max(number, 0), 10);
                    Set(() => Rating, ref _rating, number.ToString());
                }
            }
        }

        [XmlElement("my_anime_list")]
        public MyAnimeListDetails MyAnimeList
        {
            get { return _myAnimeList; }
            set { Set(() => MyAnimeList, ref _myAnimeList, value); }
        }

        [XmlAttribute("notes")]
        public string Notes
        {
            get { return _notes; }
            set
            {
                Set(() => Notes, ref _notes, value);
                if (MyAnimeList.HasId)
                    MyAnimeList.NeedsUpdating = true;
            }
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
        ///     The string representation of the status.
        /// </summary>
        [XmlIgnore]
        public string StatusString => Status.Description();

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
        public int SortedRating => string.IsNullOrEmpty(Rating) ? 13*SortedRateFlag - 2 : int.Parse(Rating);

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