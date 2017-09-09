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

        public async Task<bool> CrucialDirectoriesExist()
        {
            return new[]
            {
                await CheckDirectory(PathConfig.Unwatched, "episode"),
                await CheckDirectory(PathConfig.Watched, "watched"),
                await CheckDirectory(PathConfig.Torrents, "torrent files"),
                CheckFile(PathConfig.TorrentDownloader, "torrent client")
            }.All(b => b);






        }

        // 

        private static bool ValidPossiblePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length <= 2)
                return false;

            FileInfo fi = null;

            try
            {
                fi = new FileInfo(path);
            }

            catch (ArgumentException) { }
            catch (PathTooLongException) { }
            catch (NotSupportedException) { }

            return !ReferenceEquals(fi, null);
        }

        private static async Task<bool> CheckDirectory(string path, string title)
        {
            if (Directory.Exists(path))
                return true;

            if (ValidPossiblePath(path))
            {
                var create = await Methods.QuestionYesNo($"Your {title} folder doesn't seem to exist.\n" +
                                                         "Would you like to create it at the given path:\n\n" +
                                                         $"{path}");
                if (create)
                    Directory.CreateDirectory(path);
                else
                    return false;
            }

            else
            {
                Methods.Alert($"Your path for the {title} folder is invalid, try and enter it again.");
                return false;
            }

            return true;
        }

        private static bool CheckFile(string path, string title)
        {
            var exists = File.Exists(path) && path.ToLower().EndsWith(".exe");
            if (!exists)
            {
                Methods.Alert($"Your path for the {title} is invalid, try and enter it again.");
                return false;
            }
            return true;
        }

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