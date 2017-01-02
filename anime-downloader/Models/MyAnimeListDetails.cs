using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using anime_downloader.Classes;
using anime_downloader.Enums;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    [Serializable]
    public sealed class MyAnimeListDetails : ObservableObject
    {
        public static AnimeSeason CurrentSeason = new AnimeSeason
        {
            Year = DateTime.Now.Year,
            Season = (Season)Math.Ceiling(Convert.ToDouble(DateTime.Now.Month) / 3)
        };

        private string _english;
        private string _id;
        private bool _needsUpdating;
        private int _overallTotal;
        private string _synonyms;
        private string _synopsis;
        private string _title;
        private int _totalEpisodes;

        [XmlAttribute("id")]
        public string Id
        {
            get { return _id; }
            set
            {
                Set(() => Id, ref _id, value);
                RaisePropertyChanged(nameof(HasId));
            }
        }

        [XmlAttribute("synopsis")]
        public string Synopsis
        {
            get { return _synopsis; }
            set { Set(() => Synonyms, ref _synopsis, value); }
        }

        [XmlAttribute("image")]
        public string Image { get; set; }

        [XmlAttribute("title")]
        public string Title
        {
            get { return _title; }
            set
            {
                Set(() => Title, ref _title, value);
                RaisePropertyChanged(nameof(TitleAndEnglish));
            }
        }

        [XmlAttribute("english")]
        public string English
        {
            get { return _english; }
            set
            {
                Set(() => English, ref _english, value);
                RaisePropertyChanged(nameof(TitleAndEnglish));
            }
        }

        [XmlAttribute("synonyms")]
        public string Synonyms
        {
            get { return _synonyms; }
            set
            {
                Set(() => Synonyms, ref _synonyms, value);
                RaisePropertyChanged(nameof(SynonymsSplit));
            }
        }

        [XmlAttribute("needs_updates")]
        public bool NeedsUpdating
        {
            get { return _needsUpdating; }
            set { Set(() => NeedsUpdating, ref _needsUpdating, value); }
        }

        [XmlAttribute("total_episodes")]
        public int TotalEpisodes
        {
            get { return _totalEpisodes; }
            set
            {
                Set(() => TotalEpisodes, ref _totalEpisodes, value);
                RaisePropertyChanged(nameof(Total));
            }
        }

        [XmlAttribute("overall_total")]
        public int OverallTotal
        {
            get { return _overallTotal; }
            set
            {
                Set(() => OverallTotal, ref _overallTotal, value);
                RaisePropertyChanged(nameof(Total));
            }
        }

        [XmlAttribute("series_continuation_episode")]
        public string SeriesContinuationEpisode { get; set; }
        
        [XmlElement("aired")]
        public AnimeSeason Aired { get; set; }

        [XmlElement("ended")]
        public AnimeSeason Ended { get; set; }

        // 

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
                       && (Aired.Year < CurrentSeason.Year
                           || (Aired.Year == CurrentSeason.Year
                               && (int) Aired.Season <= (int) CurrentSeason.Season))
                       && (Ended == null
                           || (Ended.Year == CurrentSeason.Year
                               && (int) Ended.Season == (int) CurrentSeason.Season));
            }
        }

        [XmlIgnore]
        public int Total => OverallTotal > 0 ? OverallTotal : TotalEpisodes;

        [XmlIgnore]
        public bool HasId => !string.IsNullOrEmpty(Id);

        [XmlIgnore]
        public IEnumerable<string> TitleAndEnglish => new[] {Title, English};

        [XmlIgnore]
        public IEnumerable<string> SynonymsSplit => Synonyms.Split(';');
    }
}