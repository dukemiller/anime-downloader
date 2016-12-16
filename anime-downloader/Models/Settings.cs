using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using anime_downloader.Annotations;
using anime_downloader.Classes.Xml;

namespace anime_downloader.Models
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

            Paths = new PathDetails(Root.Element("path"));

            Flags = new FlagDetails(Root.Element("flag"));

            MyAnimeList = new MyAnimeListLoginDetails(Root.Element("myanimelist"));
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

        public PathDetails Paths { get; set; }

        public FlagDetails Flags { get; set; }

        public MyAnimeListLoginDetails MyAnimeList { get; set; }

        /// <summary>
        ///     The application defined sort for the anime list.
        /// </summary>
        public string SortBy
        {
            get { return Root.Element("sortBy")?.Value ?? "name"; }
            set
            {
                Root.Element("sortBy")?.SetValue(value);
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
            }
        }

        /// <summary>
        ///     Save the schema to the settings path.
        /// </summary>
        public void Save()
        {
            AnimeCollection.SaveSettings(SettingsDocument);
        }
}

    public class PathDetails
    {
        private readonly XElement _root;

        public PathDetails(XElement root)
        {
            _root = root;
        }

        /// <summary>
        ///     The path to the directory containing already watched files.
        /// </summary>
        public string WatchedDirectory
        {
            get { return _root.Element("watched")?.Value; }
            set
            {
                if (value.Equals(WatchedDirectory))
                    return;
                _root.Element("watched")?.SetValue(value);
            }
        }

        /// <summary>
        ///     The path to the directory where files will be downloaded to.
        /// </summary>
        public string EpisodeDirectory
        {
            get { return _root.Element("episode")?.Value; }
            set
            {
                if (value.Equals(EpisodeDirectory))
                    return;
                _root.Element("episode")?.SetValue(value);
            }
        }

        /// <summary>
        ///     The path to the directory where all .torrent files will be downloaded to.
        /// </summary>
        public string TorrentFilesDirectory
        {
            get { return _root.Element("torrents")?.Value; }
            set
            {
                _root.Element("torrents")?.SetValue(value);
            }
        }

        /// <summary>
        ///     The path to the utorrent executable.
        /// </summary>
        public string UtorrentFile
        {
            get { return _root.Element("utorrent")?.Value; }
            set
            {
                _root.Element("utorrent")?.SetValue(value);
            }
        }

    }

    public class FlagDetails
    {
        private readonly XElement _root;

        public FlagDetails(XElement root)
        {
            _root = root;
        }

        /// <summary>
        ///     The flag to state if the sort defined in the settings should be reversed.
        /// </summary>
        public bool SortByReversed
        {
            get { return bool.Parse(_root.Element("sortByReversed")?.Value ?? "false"); }
            set
            {
                _root.Element("sortByReversed")?.SetValue(value);
            }
        }
        
        /// <summary>
        ///     A flag defining if the program should exit on it's close (or minimize to the tray).
        /// </summary>
        public bool ExitOnClose
        {
            get { return bool.Parse(_root.Element("exitOnClose")?.Value ?? "false"); }
            set
            {
                _root.Element("exitOnClose")?.SetValue(value);
            }
        }

        /// <summary>
        ///     A flag defining if the tray icon should only be shown (instead of only when minimized).
        /// </summary>
        public bool AlwaysShowTray
        {
            get { return bool.Parse(_root.Element("alwaysShowTray")?.Value ?? "false"); }
            set
            {
                _root.Element("alwaysShowTray")?.SetValue(value);
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
                var value = _root.Element("individualShowFolders")?.Value;
                return bool.TryParse(value, out result) && result;
            }
            set
            {
                _root.Element("individualShowFolders")?.SetValue(value);
            }
        }

        /// <summary>
        ///     The user preference for only wanting anime downloaded containing whitelisted subgroups.
        /// </summary>
        public bool OnlyWhitelisted
        {
            get { return bool.Parse(_root.Element("onlyWhitelistedSubs")?.Value ?? "false"); }
            set
            {
                _root.Element("onlyWhitelistedSubs")?.SetValue(value);
            }
        }
    }

    public class MyAnimeListLoginDetails : INotifyPropertyChanged
    {
        private readonly XElement _root;

        public MyAnimeListLoginDetails(XElement root)
        {
            _root = root;
        }

        public string Username
        {
            get { return _root.Element("username")?.Value; }
            set
            {
                _root.Element("username")?.SetValue(value);
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get { return _root.Element("password")?.Value; }
            set
            {
                _root.Element("password")?.SetValue(value);
                OnPropertyChanged();
            }
        }

        public bool Works
        {
            get
            {
                bool result;
                var value = _root.Element("works")?.Value;
                return bool.TryParse(value, out result) && result;
            }

            set
            {
                _root.Element("works")?.SetValue(value);
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}