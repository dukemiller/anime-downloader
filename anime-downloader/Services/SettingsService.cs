using System.Collections.Generic;
using anime_downloader.Classes;
using anime_downloader.Models;

namespace anime_downloader.Services
{
    public class SettingsService: ISettingsService
    {
        public PathConfiguration PathConfig { get; set; }
        public FlagConfiguration FlagConfig { get; set; }
        public MyAnimeListConfiguration MyAnimeListConfig { get; set; }
        public string SortBy { get; set; }
        public string FilterBy { get; set; }
        public List<string> Subgroups { get; set; }
        public List<Anime> Anime { get; set; }
        public void Save()
        {
            throw new System.NotImplementedException();
        }
    }
}