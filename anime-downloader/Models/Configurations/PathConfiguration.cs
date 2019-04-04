using System;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class PathConfiguration : ObservableObject
    {
        /// <summary>
        ///     The path to the directory that files will initially download to.
        /// </summary>
        [JsonProperty("unwatched")]
        public string Unwatched { get; set; } = "";

        /// <summary>
        ///     The path to the directory where watched files will be moved to.
        /// </summary>
        [JsonProperty("watched")]
        public string Watched { get; set; } = "";

        /// <summary>
        ///     The path to the directory containing torrents.
        /// </summary>
        [JsonProperty("torrents")]
        public string Torrents { get; set; } = "";

        /// <summary>
        ///     The path to the torrent executable.
        /// </summary>
        [JsonProperty("torrent_downloader")]
        public string TorrentDownloader { get; set; } = "";
    }
}