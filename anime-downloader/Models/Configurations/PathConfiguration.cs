using System;
using System.IO;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class PathConfiguration: ObservableObject
    {
        private string _unwatched;
        private string _watched;
        private string _torrents;
        private string _torrentDownloader;
        private string _playlist;
        private string _logging;

        [XmlAttribute("unwatched")]
        public string Unwatched
        {
            get { return _unwatched; }
            set { Set(() => Unwatched, ref _unwatched, value); }
        }

        [XmlAttribute("watched")]
        public string Watched
        {
            get { return _watched; }
            set { Set(() => Watched, ref _watched, value); }
        }

        [XmlAttribute("torrents")]
        public string Torrents
        {
            get { return _torrents; }
            set { Set(() => Torrents, ref _torrents, value); }
        }

        [XmlAttribute("torrent_downloader")]
        public string TorrentDownloader
        {
            get { return _torrentDownloader; }
            set { Set(() => TorrentDownloader, ref _torrentDownloader, value); }
        }

        [XmlAttribute("playlist")]
        public string Playlist
        {
            get { return _playlist; }
            set { Set(() => Playlist, ref _playlist, value); }
        }

        [XmlAttribute("log")]
        public string Logging
        {
            get { return _logging; }
            set { Set(() => Logging, ref _logging, value); }
        }

        public static string DuplicatesDirectory => Path.Combine(Environment
            .GetFolderPath(Environment.SpecialFolder.MyVideos),
            "Duplicates");
    }
}