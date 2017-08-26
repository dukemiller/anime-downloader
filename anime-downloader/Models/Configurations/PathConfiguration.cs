using System;
using System.IO;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class PathConfiguration : ObservableObject
    {
        private string _torrentDownloader;

        private string _torrents;

        private string _unwatched;

        private string _watched;

        /// <summary>
        ///     The path to the directory that files will initially download to.
        /// </summary>
        [XmlAttribute("unwatched")]
        public string Unwatched
        {
            get => _unwatched;
            set
            {
                Set(() => Unwatched, ref _unwatched, value);
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     The path to the directory where watched files will be moved to.
        /// </summary>
        [XmlAttribute("watched")]
        public string Watched
        {
            get => _watched;
            set
            {
                Set(() => Watched, ref _watched, value);
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     The path to the directory containing torrents.
        /// </summary>
        [XmlAttribute("torrents")]
        public string Torrents
        {
            get => _torrents;
            set => Set(() => Torrents, ref _torrents, value);
        }

        /// <summary>
        ///     The path to the torrent executable.
        /// </summary>
        [XmlAttribute("torrent_downloader")]
        public string TorrentDownloader
        {
            get => _torrentDownloader;
            set => Set(() => TorrentDownloader, ref _torrentDownloader, value);
        }

        /// <summary>
        ///     The path to the folder containing all settings and configuration files.
        /// </summary>
        public static string ApplicationDirectory => Path.Combine(Environment
                .GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "anime_downloader");

        public static string DuplicatesDirectory => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        /// <summary>
        ///     The path to the playlist file.
        /// </summary>
        public static string Playlist => Path.Combine(ApplicationDirectory, "playlist.m3u");

        /// <summary>
        ///     The path to the log text file.
        /// </summary>
        public string Logging => Path.Combine(ApplicationDirectory, "log.txt");
    }
}