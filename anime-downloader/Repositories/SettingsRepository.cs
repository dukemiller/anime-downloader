using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.Repositories
{
    [Serializable]
    public class SettingsRepository : ObservableObject, ISettingsRepository
    {
        [JsonProperty("paths")]
        public PathConfiguration PathConfig { get; set; } = new PathConfiguration
        {
            Watched = Path.Combine(App.Path.Directory.CurrentWorking, "Watched"),
            Unwatched = Path.Combine(App.Path.Directory.CurrentWorking, "Shows"),
            Torrents = Path.Combine(App.Path.Directory.CurrentWorking, "Torrents"),
            TorrentDownloader = @"C:\Program Files (x86)\uTorrent\uTorrent.exe"
        };

        [JsonProperty("flags")]
        public FlagConfiguration FlagConfig { get; set; } = new FlagConfiguration
        {
            AlwaysShowTray = true,
            ExitOnClose = true
        };

        [JsonProperty("version")]
        public VersionCheck Version { get; set; } = new VersionCheck
        {
            LastChecked = DateTime.Now,
            NeedsUpdate = false
        };

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
            if (File.Exists(App.Path.Settings))
                using (var stream = new StreamReader(App.Path.Settings))
                    return JsonConvert.DeserializeObject<SettingsRepository>(stream.ReadToEnd());

            return new SettingsRepository();
        }

        public void Save()
        {
            try
            {
                using (var stream = new StreamWriter(App.Path.Settings))
                {
                    var data = JsonConvert.SerializeObject(this, Formatting.Indented,
                        new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});
                    stream.Write(data);
                }
            }
            catch (Exception e)
            {
                Alert($"There was an issue saving the settings:\n{e.Message}");
            }
        }

        public async Task<bool> CrucialDirectoriesExist() =>
            List.Of(
                await CheckDirectory(PathConfig.Unwatched, "episode"),
                await CheckDirectory(PathConfig.Watched, "watched"),
                await CheckDirectory(PathConfig.Torrents, "torrent files"),
                CheckExe(PathConfig.TorrentDownloader, "torrent client")
            ).All();
    }
}