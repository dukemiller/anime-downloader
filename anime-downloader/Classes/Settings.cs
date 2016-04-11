using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using anime_downloader.Classes.Xml;

namespace anime_downloader.Classes
{
    public class Settings
    {
        private XmlController _xml;

        private XContainer Root => Xml.SettingsRoot;

        private XmlController Xml => _xml ?? (_xml = XmlController.GetXmlController(this));

        /// <summary>
        ///     Where all XML files are stored.
        /// </summary>
        public string ApplicationPath
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "anime-downloader");

        /// <summary>
        ///     The path of the settings XML file.
        /// </summary>
        public string SettingsXmlPath => Path.Combine(ApplicationPath, "settings.xml");

        /// <summary>
        ///     The path of the anime XML file.
        /// </summary>
        public string AnimeXmlPath => Path.Combine(ApplicationPath, "anime.xml");

        /// <summary>
        ///     The path of watched files.
        /// </summary>
        public string WatchedPath => Path.Combine(BaseFolderPath, "Watched");

        /// <summary>
        ///     The path of duplicate files.
        /// </summary>
        public string DuplicatesPath => Path.Combine(BaseFolderPath, "Duplicates");

        /// <summary>
        ///     The path of base downloading folder.
        /// </summary>
        public string BaseFolderPath
        {
            get { return Root.Element("path")?.Element("base")?.Value; }
            set
            {
                if (value.Equals(BaseFolderPath))
                    return;
                Root.Element("path")?.Element("base")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The path where all .torrent files will be downloaded to.
        /// </summary>
        public string TorrentFilesPath
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
        public string UtorrentPath
        {
            get { return Root.Element("path")?.Element("utorrent")?.Value; }
            set
            {
                Root.Element("path")?.Element("utorrent")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     A path to where the playlist will be created.
        /// </summary>
        public string LoggingPath => Path.Combine(BaseFolderPath, "playlist.m3u");

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

        public string LogPath => Path.Combine(BaseFolderPath, "log.txt");

        private void Save()
        {
            if (!Xml.AutoSave)
                return;
            Xml.SaveSettings();
        }

        private void Backup()
        {
            
        }

        /// <summary>
        ///     The paths where the episodes are stored.
        /// </summary>
        /// <remarks>
        ///     The path will always start with a "2" since it starts with the year,
        ///     so this will need updating in about 984 years or any time named directories change.
        /// </remarks>
        /// <returns></returns>
        public IEnumerable<string> EpisodePaths(bool includeWatched = false) => Directory
            .GetDirectories(BaseFolderPath)
            .Where(folder =>
            {
                var path = Path.GetFileName(folder);
                return path != null &&
                       (path.StartsWith("2") ||
                        !path.ToLower().Equals("torrents") &&
                        (includeWatched & path.ToLower().Equals("watched")) &&
                        !path.ToLower().Equals("duplicates"));
            });

        /// <summary>
        ///     Create and return the path to a folder based on a timestamp of the current moment.
        /// </summary>
        /// <returns>A path used to download into.</returns>
        public string GetEpisodeFolder()
        {
            var date = DateTime.Now;
            var week = Math.Floor(Convert.ToDouble(date.DayOfYear)/7);
            var folder = $"{date.Year} - Week {week} - {date.ToString("MMMM")}";
            var path = Path.Combine(BaseFolderPath, folder);
            return path;
        }
    }
}