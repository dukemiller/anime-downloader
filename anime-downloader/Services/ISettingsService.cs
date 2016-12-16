using System.Collections.Generic;
using anime_downloader.Classes;
using anime_downloader.Models;

namespace anime_downloader.Services
{
    public interface ISettingsService
    {
        PathConfiguration PathConfig { get; set; }
        FlagConfiguration FlagConfig { get; set; }
        MyAnimeListConfiguration MyAnimeListConfig { get; set; }

        string SortBy { get; set; }
        string FilterBy { get; set; }
        List<string> Subgroups { get; set; }
        List<Anime> Anime { get; set; }

        void Save();
    }
}