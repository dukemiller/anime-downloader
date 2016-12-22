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
        
        /// <summary>
        ///     The path to the folder containing all settings and configuration files.
        /// </summary>
        private static string ApplicationDirectory => Path.Combine(Environment
            .GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "anime_downloader");

        public string DuplicatesDirectory => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        /// <summary>
        ///     The path to the playlist file.
        /// </summary>
        public string Playlist => Path.Combine(ApplicationDirectory, "playlist.m3u");

        /// <summary>
        ///     The path to the log text file.
        /// </summary>
        public string Logging => Path.Combine(ApplicationDirectory, "log.txt");

    }
}