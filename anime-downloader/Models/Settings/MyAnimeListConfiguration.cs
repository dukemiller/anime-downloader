using System;
using System.Xml.Serialization;

namespace anime_downloader.Models
{
    [Serializable]
    public class MyAnimeListConfiguration
    {
        [XmlAttribute("username")]
        public string Username { get; set; }

        [XmlAttribute("password")]
        public string Password { get; set; }

        [XmlAttribute("working")]
        public bool Works { get; set; }
    }
}