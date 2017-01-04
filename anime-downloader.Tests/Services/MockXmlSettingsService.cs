using System.Collections.Generic;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Tests.Services
{
    public class MockXmlSettingsService: ISettingsService
    {
        public PathConfiguration PathConfig { get; set; } = new PathConfiguration();
        public FlagConfiguration FlagConfig { get; set; } = new FlagConfiguration();
        public MyAnimeListConfiguration MyAnimeListConfig { get; set; } = new MyAnimeListConfiguration();
        public string SortBy { get; set; } = "";
        public string FilterBy { get; set; } = "";
        public List<string> Subgroups { get; set; } = new List<string>();
        public List<Anime> Animes { get; set; } = new List<Anime>();
        public bool CrucialDirectoriesExist() => true;
        public void Save() {}
    }
}
