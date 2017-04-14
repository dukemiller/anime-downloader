using System;
using System.Collections.Generic;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;

namespace anime_downloader.Services.Interfaces
{
    public interface ISettingsService
    {
        PathConfiguration PathConfig { get; set; }
        FlagConfiguration FlagConfig { get; set; }
        MyAnimeListConfiguration MyAnimeListConfig { get; set; }
        VersionCheck Version { get; set; }

        string SortBy { get; set; }
        string FilterBy { get; set; }
        List<string> Subgroups { get; set; }
        List<Anime> Animes { get; set; }

        bool CrucialDirectoriesExist();

        void Save();
    }
}