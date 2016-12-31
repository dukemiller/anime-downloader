using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace anime_downloader.Models.MyAnimeList
{
    [Serializable]
    [XmlRoot("anime")]
    public class FindResultRoot
    {
        [XmlElement("entry")]
        public List<FindResult> Entries { get; set; }
    }
}