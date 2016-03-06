using System.IO;

namespace anime_downloader.Classes {
    public class Settings {
        /// <summary>
        ///     The user preference for only wanting anime downloaded containing whitelisted subgroups.
        /// </summary>
        public bool OnlyWhitelisted;

        /// <summary>
        ///     The user preferred subgroups.
        /// </summary>
        public string[] Subgroups;

        /// <summary>
        ///     Where all XML files are stored.
        /// </summary>
        public string ApplicationPath { get; set; }

        /// <summary>
        ///     The path of the settings XML file.
        /// </summary>
        public string SettingsXmlPath => Path.Combine(ApplicationPath, "settings.xml");

        /// <summary>
        ///     The path of the anime XML file.
        /// </summary>
        public string AnimeXmlPath => Path.Combine(ApplicationPath, "anime.xml");

        /// <summary>
        ///     The path of base downloading folder.
        /// </summary>
        public string BaseFolderPath { get; set; }

        /// <summary>
        ///     The path where all .torrent files will be downloaded to.
        /// </summary>
        public string TorrentFilesPath { get; set; }

        /// <summary>
        ///     The path to the utorrent executable.
        /// </summary>
        public string UtorrentPath { get; set; }

        /// <summary>
        ///     The user preferred anime list sort method.
        /// </summary>
        public string SortBy { get; set; } = "name";
    }
}