using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace anime_downloader.Models.MyAnimeList
{
    [Serializable]
    [XmlRoot("entry")]
    public class FindResult
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("english")]
        public string English { get; set; }

        [XmlElement("synonyms")]
        public string Synonyms { get; set; }

        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("episodes")]
        public int TotalEpisodes { get; set; }

        [XmlElement("score")]
        public string Score { get; set; }

        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("status")]
        public string Status { get; set; }

        [XmlElement("start_date")]
        public string StartDate { get; set; }

        [XmlElement("end_date")]
        public string EndDate { get; set; }

        [XmlElement("synopsis")]
        public string Synopsis { get; set; }

        [XmlElement("image")]
        public string Image { get; set; }
    }
}