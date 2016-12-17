using System.Collections.Generic;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using Settings = anime_downloader.Properties.Settings;

namespace anime_downloader.Services
{
    public class SettingsService: ISettingsService
    {
        public SettingsService()
        {
            if (Settings.Default.Animes == null)
                Settings.Default.Animes = new List<Anime>();
            if (Settings.Default.Subgroups == null)
                Settings.Default.Subgroups = new List<string>();

            if (Settings.Default.PathConfiguration == null)
                Settings.Default.PathConfiguration = new PathConfiguration();
            if (Settings.Default.FlagConfiguration == null)
                Settings.Default.FlagConfiguration = new FlagConfiguration();
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
    }
}