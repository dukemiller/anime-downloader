using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    [Serializable]
    public sealed class MyAnimeListDetails : ObservableObject
    {
        private string _english;
        private bool _needsUpdating;
        private string _id;
        private string _title;
        private int _totalEpisodes;
        private int _overallTotal;

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
        public string Synopsis { get; set; }

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
        public string Synonyms { get; set; }

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

        [XmlAttribute("overall_title")]
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
        public int? SeriesContinuationEpisode { get; set; }

        // 

        public int Total => OverallTotal > 0 ? OverallTotal : TotalEpisodes;

        public bool HasId => !string.IsNullOrEmpty(Id);

        public IEnumerable<string> TitleAndEnglish => new[] { Title, English };

        public IEnumerable<string> SynonymsSplit => Synonyms.Split(';');
    }
}