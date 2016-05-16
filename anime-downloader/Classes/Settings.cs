using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using anime_downloader.Classes.Xml;

namespace anime_downloader.Classes
{
    public class Settings
    {
        private XmlController _xml;
        public bool Loaded { get; set; } = false;

        private XContainer Root => Xml.SettingsRoot;

        private XmlController Xml => _xml ?? (_xml = XmlController.GetXmlController(this));

        /// <summary>
        ///     Where all XML and settings files are stored.
        /// </summary>
        public string ApplicationDirectory
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "anime-downloader");

        /// <summary>
        ///     The path of the settings XML file.
        /// </summary>
        public string SettingsXml => Path.Combine(ApplicationDirectory, "settings.xml");

        /// <summary>
        ///     The path of the anime XML file.
        /// </summary>
        public string AnimeXml => Path.Combine(ApplicationDirectory, "anime.xml");

        /// <summary>
        ///     The path of watched files.
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
        ///     Get the user defined download folder.
        /// </summary>
        /// <returns>A path used to download into.</returns>
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
        ///     The path of duplicate files.
        /// </summary>
        public string DuplicatesDirectory
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Duplicates");

        /// <summary>
        ///     The path where all .torrent files will be downloaded to.
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
        ///     A path to where the playlist will be created.
        /// </summary>
        public string PlaylistFile => Path.Combine(ApplicationDirectory, "playlist.m3u");

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

        public string LoggingFile => Path.Combine(ApplicationDirectory, "log.txt");

        /// <summary>
        ///     The user preferred anime list sort method.
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

        public bool SortByReversed
        {
            get { return bool.Parse(Root.Element("flag")?.Element("sortByReversed")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("sortByReversed")?.SetValue(value);
                Save();
            }
        }

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

        public bool ExitOnClose
        {
            get { return bool.Parse(Root.Element("flag")?.Element("exitOnClose")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("exitOnClose")?.SetValue(value);
                Save();
            }
        }

        public bool AlwaysShowTray
        {
            get { return bool.Parse(Root.Element("flag")?.Element("alwaysShowTray")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("alwaysShowTray")?.SetValue(value);
                Save();
            }
        }

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

        public string GroupDownloadBy
        {
            get { return Root.Element("groupDownloadBy")?.Value ?? "PerWeek"; }
            set
            {
                Root.Element("groupDownloadBy")?.SetValue(value);
                Save();
            }
        }

        private void Save()
        {
            if (!Xml.AutoSave)
                return;
            Xml.SaveSettings();
        }
    }
}