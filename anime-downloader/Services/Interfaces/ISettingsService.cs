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

        bool CrucialDirectoriesExist();

        string SortBy { get; set; }
        string FilterBy { get; set; }
        List<string> Subgroups { get; set; }
        List<Anime> Animes { get; set; }

        void Save();
    }
}