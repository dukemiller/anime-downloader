using System;
using System.Xml.Serialization;
using anime_downloader.Classes;
using anime_downloader.Enums;

namespace anime_downloader.Models
{
    [Serializable]
    public class AnimeSeason
    {
        [XmlAttribute("Year")]
        public int Year { get; set; }

        [XmlAttribute("Season")]
        public Season Season { get; set; }

        [XmlIgnore]
        public int Sort => Year - 4 + (int) Season;

        [XmlIgnore]
        public string Title => $"{Season.Description()} {Year}";
    }
}
