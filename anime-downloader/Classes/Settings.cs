using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using anime_downloader.Classes.Xml;

namespace anime_downloader.Classes {
    public class Settings {
        
        private XContainer Root => Xml.SettingsRoot();

        private XmlController _xml;

        private XmlController Xml => _xml ?? (_xml = XmlController.GetXmlController(this));

        private void Save() {
            if (!Xml.AutoSave)
                return;
            Xml.SaveSettings();
        }

        /// <summary>
        ///     Where all XML files are stored.
        /// </summary>
        public string ApplicationPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
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
        ///     The path of base downloading folder.
        /// </summary>
        public string BaseFolderPath {
            get { return Root.Element("path")?.Element("base")?.Value; }
            set {
                if (value.Equals(BaseFolderPath))
                    return;
                Root.Element("path")?.Element("base")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The path where all .torrent files will be downloaded to.
        /// </summary>
        public string TorrentFilesPath {
            get { return Root.Element("path")?.Element("torrents")?.Value; }
            set {
                Root.Element("path")?.Element("torrents")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The path to the utorrent executable.
        /// </summary>
        public string UtorrentPath {
            get { return Root.Element("path")?.Element("utorrent")?.Value; }
            set {
                Root.Element("path")?.Element("utorrent")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The user preferred anime list sort method.
        /// </summary>
        public string SortBy {
            get { return Root.Element("sortBy")?.Value ?? "name"; }
            set {
                Root.Element("sortBy")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The user preference for only wanting anime downloaded containing whitelisted subgroups.
        /// </summary>
        public bool OnlyWhitelisted {
            get { return bool.Parse(Root.Element("flag")?.Element("only-whitelisted-subs")?.Value ?? "false"); }
            set {
                Root.Element("flag")?.Element("only-whitelisted-subs")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The user preferred subgroups.
        /// </summary>
        public string[] Subgroups {
            get { return Root.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray(); }
            set {
                //TODO
                Save();
            }
        }

    }
}