using System;
using System.Xml.Serialization;

namespace anime_downloader.Models
{
    [Serializable]
    public class PathConfiguration
    {
        [XmlAttribute("unwatched")]
        public string Unwatched { get; set; }

        [XmlAttribute("watched")]
        public string Watched { get; set; }

        [XmlAttribute("torrents")]
        public string Torrents { get; set; }

        [XmlAttribute("torrent_downloader")]
        public string TorrentDownloader { get; set; }

        [XmlAttribute("playlist")]
        public string Playlist { get; set; }

        [XmlAttribute("log")]
        public string Logging { get; set; }
    }
}