using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace anime_downloader.Classes {

    public class Settings {

        /// <summary>
        /// Where all XML files are stored.
        /// </summary>
        public string applicationPath { get; set; }

        /// <summary>
        /// The path of the settings XML file.
        /// </summary>
        public string settingsXMLPath { get; set; }

        /// <summary>
        /// The path of the anime XML file.
        /// </summary>
        public string animeXMLPath { get; set; }

        /// <summary>
        /// The path of base downloading folder.
        /// </summary>
        public string baseFolderPath { get; set; }

        /// <summary>
        /// The path where all .torrent files will be downloaded to.
        /// </summary>
        public string torrentFilesPath { get; set; }

        /// <summary>
        /// The path to the utorrent executable.
        /// </summary>
        public string utorrentPath { get; set; }

        /// <summary>
        /// The user preference for only wanting anime downloaded containing whitelisted subgroups.
        /// </summary>
        public bool onlyWhitelisted;

        /// <summary>
        /// The user preferred subgroups.
        /// </summary>
        public string[] subgroups;

        /// <summary>
        /// The user preferred anime list sort method.
        /// </summary>
        public string sortBy { get; set; } = "name";
    }
}
