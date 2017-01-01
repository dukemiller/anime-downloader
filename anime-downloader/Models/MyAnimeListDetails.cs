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
        private string _english;
        private string _id;
        private bool _needsUpdating;
        private int _overallTotal;
        private string _synonyms;
        private string _synopsis;
        private string _title;
        private string _seasonInformation;
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
        
        // 

        [XmlIgnore]
        public int SeasonSort => Aired?.Sort ?? (DateTime.Now.Year + 3) * Anime.SortedAiredFlag - 2;

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