using anime_downloader.Classes.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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
        public string WatchedDirectory => Path.Combine(BaseDirectory, "Watched");

        /// <summary>
        ///     The path of duplicate files.
        /// </summary>
        public string DuplicatesDirectory => Path.Combine(BaseDirectory, "Duplicates");

        /// <summary>
        ///     The path of base downloading folder.
        /// </summary>
        public string BaseDirectory
        {
            get { return Root.Element("path")?.Element("base")?.Value; }
            set
            {
                if (value.Equals(BaseDirectory))
                    return;
                Root.Element("path")?.Element("base")?.SetValue(value);
                Save();
            }
        }

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
        public string PlaylistFile => Path.Combine(BaseDirectory, "playlist.m3u");

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

        public string LoggingFile => Path.Combine(BaseDirectory, "log.txt");

        // public string BackupPath => Path.Combine(ApplicationPath, "Backup");

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
            get { return bool.Parse(Root.Element("flag")?.Element("only-whitelisted-subs")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("only-whitelisted-subs")?.SetValue(value);
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
        ///     The user preference if there should be a log file in the folder detailing
        ///     when files are downloaded.
        /// </summary>
        public bool UseLogging
        {
            get { return bool.Parse(Root.Element("flag")?.Element("use-logging")?.Value ?? "false"); }
            set
            {
                Root.Element("flag")?.Element("use-logging")?.SetValue(value);
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

        // private void Backup(){}

        /// <summary>
        ///     The paths where the episodes are stored.
        /// </summary>
        /// <remarks>
        ///     The path will always start with a "2" since it starts with the year,
        ///     so this will need updating in about 984 years or any time named directories change.
        /// </remarks>
        /// <returns></returns>
        public IEnumerable<string> EpisodeDirectories(bool includeWatched = false) => Directory
            .GetDirectories(BaseDirectory)
            .Where(folder =>
            {
                var path = Path.GetFileName(folder)?.ToLower();
                return path != null &&
                       !path.Equals("torrents") &&
                       !path.Equals("duplicates") &&
                       (includeWatched || !path.Equals("watched"));
            });

        /// <summary>
        ///     Create and return the path to a folder based on a timestamp of the current moment.
        /// </summary>
        /// <returns>A path used to download into.</returns>
        public string GetDownloadFolder()
        {
            if (GroupDownloadBy.Equals("PerWeek"))
            {
                var date = DateTime.Now;
                var week = Math.Floor(Convert.ToDouble(date.DayOfYear) / 7);
                var folder = $"{date.Year} - Week {week} - {date.ToString("MMMM")}";
                var path = Path.Combine(BaseDirectory, folder);
                return path;
            }

            return Path.Combine(BaseDirectory, "Shows");
        }
    }
}