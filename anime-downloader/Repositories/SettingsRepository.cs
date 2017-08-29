using System;
using System.Collections.Generic;
using System.IO;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Repositories
{
    [Serializable]
    public class SettingsRepository: ObservableObject, ISettingsRepository
    {
        [JsonIgnore]
        public static string SettingsPath => Path.Combine(PathConfiguration.ApplicationDirectory, "settings.json");

        [JsonIgnore]
        public static string ImageDirectory => Path.Combine(PathConfiguration.ApplicationDirectory, "images");

        // 

        public SettingsRepository()
        {
            DefaultValues();
        }

        // 

        [JsonProperty("paths")]
        public PathConfiguration PathConfig { get; set; }

        [JsonProperty("flags")]
        public FlagConfiguration FlagConfig { get; set; }

        [JsonProperty("version")]
        public VersionCheck Version { get; set; }

        [JsonProperty("provider")]
        public DownloadProvider Provider { get; set; } = DownloadProvider.NyaaSi;

        [JsonProperty("sort_by")]
        public string SortBy { get; set; } = "name";

        [JsonProperty("filter_by")]
        public string FilterBy { get; set; } = "";

        [JsonProperty("subgroups")]
        public List<string> Subgroups { get; set; } = new List<string>();

        // 

        public static SettingsRepository Load()
        {
            if (File.Exists(SettingsPath))
                using (var stream = new StreamReader(SettingsPath))
                    return JsonConvert.DeserializeObject<SettingsRepository>(stream.ReadToEnd());

            return new SettingsRepository();
        }

        public void Save()
        {
            using (var stream = new StreamWriter(SettingsPath))
                stream.WriteAsync(JsonConvert.SerializeObject(this, Formatting.Indented));
        }

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

        // 

        private void DefaultValues()
        {
            var path = Directory.GetCurrentDirectory();

            PathConfig = new PathConfiguration
            {
                Watched = Path.Combine(path, "Watched"),
                Unwatched = Path.Combine(path, "Shows"),
                Torrents = Path.Combine(path, "Torrents"),
                TorrentDownloader = @"C:\Program Files (x86)\uTorrent\uTorrent.exe"
            };

            FlagConfig = new FlagConfiguration
            {
                AlwaysShowTray = true,
                ExitOnClose = true
            };

            Version = new VersionCheck { LastChecked = DateTime.Now, NeedsUpdate = false };
        }
        
    }
}