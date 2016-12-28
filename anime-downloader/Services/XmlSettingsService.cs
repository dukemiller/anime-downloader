using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    [Serializable]
    [XmlRoot("Settings")]
    public class XmlSettingsService : ISettingsService
    {
        private static readonly string SettingsPath = Path.Combine(PathConfiguration.ApplicationDirectory,
            "settings.xml");

        private static bool _reading;

        // 

        public XmlSettingsService()
        {
            if (!_reading && File.Exists(SettingsPath))
            {
                _reading = true;
                using (var sw = new StreamReader(SettingsPath))
                {
                    var xmls = new XmlSerializer(typeof(XmlSettingsService));
                    var settings = xmls.Deserialize(sw) as XmlSettingsService;
                    PathConfig = settings?.PathConfig;
                    FlagConfig = settings?.FlagConfig;
                    MyAnimeListConfig = settings?.MyAnimeListConfig;
                    SortBy = settings?.SortBy;
                    FilterBy = settings?.FilterBy;
                    Subgroups = settings?.Subgroups;
                    Animes = settings?.Animes;
                }
            }

            else
            {
                PathConfig = new PathConfiguration();
                FlagConfig = new FlagConfiguration {AlwaysShowTray = true, ExitOnClose = true};
                MyAnimeListConfig = new MyAnimeListConfiguration();
                Animes = new List<Anime>();
                Subgroups = new List<string>();
                SortBy = "name";
                FilterBy = "";
            }
        }

        // 

        [XmlElement("Paths")]
        public PathConfiguration PathConfig { get; set; }

        [XmlElement("Flags")]
        public FlagConfiguration FlagConfig { get; set; }

        [XmlElement("MyAnimeList")]
        public MyAnimeListConfiguration MyAnimeListConfig { get; set; }

        [NeedsUpdating]
        public bool CrucialDirectoriesExist()
        {
            var error = string.Empty;

            if (!Directory.Exists(PathConfig.Unwatched))
                error += "Your episode folder doesn't seem to exist.\n";

            if (!Directory.Exists(PathConfig.Watched))
                error += "Your watched folder doesn't seem to exist.\n";

            if (!Directory.Exists(PathConfig.Torrents))
                error += "Your torrent files folder doesn't seem to exist.\n";

            if (!File.Exists(PathConfig.TorrentDownloader) || !PathConfig.TorrentDownloader.ToLower().EndsWith(".exe"))
                error += "Your uTorrent.exe path seems to be wrong.";

            // if (error.Length > 0)
            //     Methods.Alert(error);

            return error.Length == 0;
        }

        public string SortBy { get; set; }

        public string FilterBy { get; set; }

        public List<string> Subgroups { get; set; }

        public List<Anime> Animes { get; set; }

        // 

        public void Save()
        {
            using (var sw = new StreamWriter(SettingsPath))
            {
                var xmls = new XmlSerializer(typeof(XmlSettingsService));
                xmls.Serialize(sw, this);
            }
        }
    }
}