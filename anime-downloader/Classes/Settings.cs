using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using anime_downloader.Classes.Xml;

namespace anime_downloader.Classes
{
    public class Settings
    {
        public Settings(bool loadDefaultSettings = false)
        {
            if (loadDefaultSettings)
            {
                SettingsDocument = XDocument.Load(SettingsXml);
                AnimeDocument = XDocument.Load(AnimeXml);
            }

            else
            {
                SettingsDocument = Schema.SettingsDocument();
                AnimeDocument = Schema.AnimeDocument();
            }
        }

        /// <summary>
        ///     The root to the settings document (used for convienence).
        /// </summary>
        private XContainer Root => SettingsDocument.Root;

        /// <summary>
        ///     The XML document containing all settings information.
        /// </summary>
        public XDocument SettingsDocument { get; }

        /// <summary>
        ///     The XML document containing all anime show information.
        /// </summary>
        public XDocument AnimeDocument { get; private set; }

        /// <summary>
        ///     The path to the folder containing all settings and configuration files.
        /// </summary>
        public static string ApplicationDirectory => Path.Combine(Environment
            .GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "anime-downloader");

        /// <summary>
        ///     The path to the settings XML file.
        /// </summary>
        public static string SettingsXml => Path.Combine(ApplicationDirectory, "settings.xml");

        /// <summary>
        ///     The path to the anime XML file.
        /// </summary>
        public static string AnimeXml => Path.Combine(ApplicationDirectory, "anime.xml");

        /// <summary>
        ///     The path to the playlist file.
        /// </summary>
        public static string PlaylistFile => Path.Combine(ApplicationDirectory, "playlist.m3u");

        /// <summary>
        ///     The path to the log text file.
        /// </summary>
        public static string LoggingFile => Path.Combine(ApplicationDirectory, "log.txt");

        /// <summary>
        ///     The path to the directory where duplicate videos will be moved to
        /// </summary>
        public static string DuplicatesDirectory => Path.Combine(Environment
            .GetFolderPath(Environment.SpecialFolder.MyVideos),
            "Duplicates");

        /// <summary>
        ///     The path to the directory containing already watched files.
        /// </summary>
        public string WatchedDirectory
        {
            get { return Root.Element("path")?.Element("watched")?.Value; }
            set
            {
                if (value.Equals(WatchedDirectory))
                    return;
                Root.Element("path")?.Element("watched")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The path to the directory where files will be downloaded to.
        /// </summary>
        public string EpisodeDirectory
        {
            get { return Root.Element("path")?.Element("episode")?.Value; }
            set
            {
                if (value.Equals(EpisodeDirectory))
                    return;
                Root.Element("path")?.Element("episode")?.SetValue(value);
                Save();
            }
        }
        
        /// <summary>
        ///     The path to the directory where all .torrent files will be downloaded to.
        /// </summary>
        public string TorrentFilesDirectory
        {
            get { return Root.Element("path")?.Element("torrents")?.Value; }
            set
            {
                Root.Element("path")?.Element("torrents")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The path to the utorrent executable.
        /// </summary>
        public string UtorrentFile
        {
            get { return Root.Element("path")?.Element("utorrent")?.Value; }
            set
            {
                Root.Element("path")?.Element("utorrent")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The application defined sort for the anime list.
        /// </summary>
        public string SortBy
        {
            get { return Root.Element("sortBy")?.Value ?? "name"; }
            set
            {
                Root.Element("sortBy")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The flag to state if the sort defined in the settings should be reversed.
        /// </summary>
        public bool SortByReversed
        {
            get { return bool.Parse(Root.Element("flag")?.Element("sortByReversed")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("sortByReversed")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The application defined filter for the anime list.
        /// </summary>
        public string FilterBy
        {
            get { return Root.Element("filterBy")?.Value; }
            set
            {
                Root.Element("filterBy")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The user preference for only wanting anime downloaded containing whitelisted subgroups.
        /// </summary>
        public bool OnlyWhitelisted
        {
            get { return bool.Parse(Root.Element("flag")?.Element("onlyWhitelistedSubs")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("onlyWhitelistedSubs")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The user preferred subgroups.
        /// </summary>
        public string[] Subgroups
        {
            get { return Root.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray(); }
            set
            {
                Root.Elements("subgroup").Elements("name").Remove();
                foreach (var subgroup in value)
                {
                    Root.Element("subgroup")?.Add(new XElement("name", subgroup));
                }
                Save();
            }
        }

        /// <summary>
        ///     A flag defining if the program should exit on it's close (or minimize to the tray).
        /// </summary>
        public bool ExitOnClose
        {
            get { return bool.Parse(Root.Element("flag")?.Element("exitOnClose")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("exitOnClose")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     A flag defining if the tray icon should only be shown (instead of only when minimized).
        /// </summary>
        public bool AlwaysShowTray
        {
            get { return bool.Parse(Root.Element("flag")?.Element("alwaysShowTray")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("alwaysShowTray")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     A flag defining if each show should be put into a separate folder (instead of just being put in the main folder).
        /// </summary>
        public bool IndividualShowFolders
        {
            get
            {
                bool result;
                var value = Root.Element("flag")?.Element("individualShowFolders")?.Value;
                return bool.TryParse(value, out result) && result;
            }
            set
            {
                Root.Element("flag")?.Element("individualShowFolders")?.SetValue(value);
                Save();
            }
        }
        
        /// <summary>
        ///     Save the schema to the settings path.
        /// </summary>
        public void Save() => AnimeCollection.SaveSettings(SettingsDocument);
    }
}