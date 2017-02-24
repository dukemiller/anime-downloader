using System;
using System.Collections.Generic;
using System.IO;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Properties;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class ApplicationSettingsService : ISettingsService
    {
        public ApplicationSettingsService()
        {
            if (Settings.Default.Animes == null)
                Settings.Default.Animes = new List<Anime>();
            if (Settings.Default.Subgroups == null)
                Settings.Default.Subgroups = new List<string>();

            if (Settings.Default.PathConfiguration == null)
                Settings.Default.PathConfiguration = new PathConfiguration();
            if (Settings.Default.FlagConfiguration == null)
                Settings.Default.FlagConfiguration = new FlagConfiguration {AlwaysShowTray = true, ExitOnClose = false};
            if (Settings.Default.MyAnimeListConfiguration == null)
                Settings.Default.MyAnimeListConfiguration = new MyAnimeListConfiguration();
        }

        public PathConfiguration PathConfig
        {
            get { return Settings.Default.PathConfiguration; }
            set { Settings.Default.PathConfiguration = value; }
        }

        public FlagConfiguration FlagConfig
        {
            get { return Settings.Default.FlagConfiguration; }
            set { Settings.Default.FlagConfiguration = value; }
        }

        public MyAnimeListConfiguration MyAnimeListConfig
        {
            get { return Settings.Default.MyAnimeListConfiguration; }
            set { Settings.Default.MyAnimeListConfiguration = value; }
        }

        // 

        public List<string> Subgroups
        {
            get { return Settings.Default.Subgroups; }
            set { Settings.Default.Subgroups = value; }
        }

        public List<Anime> Animes
        {
            get { return Settings.Default.Animes; }
            set { Settings.Default.Animes = value; }
        }

        // 

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

        public string SortBy
        {
            get { return Settings.Default.SortBy; }
            set { Settings.Default.SortBy = value; }
        }

        public string FilterBy
        {
            get { return Settings.Default.FilterBy; }
            set { Settings.Default.FilterBy = value; }
        }

        // 

        public void Save() => Settings.Default.Save();

        public DateTime UpdateCheckDelay { get; set; }
    }
}