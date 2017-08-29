using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories;
using anime_downloader.Services.Interfaces;
using Newtonsoft.Json;


namespace anime_downloader.Services
{
    public class AniListService : IFindSeasonAnimeService
    {
        private readonly IAniListApi _api;

        private readonly AniListData _data;

        // 

        public AniListService(IAniListApi api)
        {
            _api = api;
            _data = AniListData.Load();
        }

        // 
        
        public async Task<List<AiringAnime>> New(AnimeSeason animeSeason)
        {
            return await _data.New(AnimeSeason.Current, async () => await _api.GetNewAnimes(animeSeason));
        }

        public async Task<List<AiringAnime>> Leftover(AnimeSeason animeSeason)
        {
            return await _data.Leftovers(AnimeSeason.Current, async () => await _api.GetLeftoverAnime(animeSeason));
        }

        public async Task FillInDetails(AnimeSeason season, bool isNew, AiringAnime anime)
        {
            anime.Description = "Loading ...";

            var updated = await _api.GetAnime(anime.Id, true);

            if (updated != null)
            {
                await _data.UpdateEntry(season, isNew, updated);
                UpdateObservableProperties(anime, updated);
            }

            else
                anime.Description = "";
        }

        // 

        /// <summary>
        ///     It's unfortunate that by updating these properties via reflection
        ///     i'm not raising propertychanged, so i'll just do it here manually
        /// </summary>
        private static void UpdateObservableProperties(AiringAnime original, AiringAnime updated)
        {
            original.Description = updated.Description;
            original.Source = updated.Source;
            original.Studio = updated.Studio;
            original.ImagePath = updated.ImagePath;
        }
    }

    [Serializable]
    internal class AniListData
    {
        private static readonly WebClient Downloader = new WebClient();

        static AniListData()
        {
            Downloader.Headers["User-Agent"] = ApiKeys.AniListUserAgent;
        }

        private AniListData() { }

        [JsonIgnore]
        private static readonly string SettingsPath = Path.Combine(PathConfiguration.ApplicationDirectory, "airing_shows.json");
        
        [JsonProperty("data")]
        public Dictionary<string, AniListSeasonData> Data { get; set; } = new Dictionary<string, AniListSeasonData>();

        public async Task<List<AiringAnime>> New(AnimeSeason season, Func<Task<List<AiringAnime>>> getNew)
        {
            if (!Data.ContainsKey(season.Title))
                Data[season.Title] = new AniListSeasonData();

            var data = Data[season.Title];
            
            // Check for any updates?
            if ((data.LastCheckedNew - DateTime.Now).Hours > 12) ;

            else if (data.New.Count == 0)
            {
                Data[season.Title].New = await getNew();
                Data[season.Title].LastCheckedNew = DateTime.Now;
                await Save();
            }

            return Data[season.Title].New;
        }

        public async Task<List<AiringAnime>> Leftovers(AnimeSeason season, Func<Task<List<AiringAnime>>> getLeftovers)
        {
            if (!Data.ContainsKey(season.Title))
                Data[season.Title] = new AniListSeasonData();

            var data = Data[season.Title];

            // Check for any updates?
            if ((data.LastCheckedLeftover - DateTime.Now).Hours > 12) ;

            if (data.LeftOver.Count == 0)
            {
                Data[season.Title].LeftOver = await getLeftovers();
                Data[season.Title].LastCheckedLeftover = DateTime.Now;
                await Save();
            }

            return Data[season.Title].LeftOver;
        }

        public async Task UpdateEntry(AnimeSeason season, bool isNew, AiringAnime updated)
        {
            if (!Data.ContainsKey(season.Title))
                return;

            var list = isNew ? Data[season.Title].New : Data[season.Title].LeftOver;
            var index = list.IndexOf(list.First(a => a.Id.Equals(updated.Id)));
            list[index] = updated;

            await DownloadImage(updated);
            await Save();
        }

        private static async Task DownloadImage(AiringAnimeSmall anime)
        {
            var image = anime.ImageUrlLge;
            var downloadPath = Path.Combine(SettingsRepository.ImageDirectory, $"{anime.Id}.png");
            if (!File.Exists(downloadPath))
                await Downloader.DownloadFileTaskAsync(image, downloadPath);
            anime.ImagePath = downloadPath;
        }

        public static AniListData Load()
        {
            if (!Directory.Exists(SettingsRepository.ImageDirectory))
                Directory.CreateDirectory(SettingsRepository.ImageDirectory);

            if (File.Exists(SettingsPath))
                using (var stream = new StreamReader(SettingsPath))
                    return JsonConvert.DeserializeObject<AniListData>(stream.ReadToEnd());

            return new AniListData();
        }

        public async Task Save()
        {
            using (var stream = new StreamWriter(SettingsPath))
                await stream.WriteAsync(JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    [Serializable]
    internal class AniListSeasonData
    {
        [JsonProperty("last_checked_new")]
        public DateTime LastCheckedNew { get; set; } = DateTime.MinValue;

        [JsonProperty("last_checked_leftover")]
        public DateTime LastCheckedLeftover { get; set; } = DateTime.MinValue;

        [JsonProperty("new")]
        public List<AiringAnime> New { get; set; } = new List<AiringAnime>();

        [JsonProperty("leftover")]
        public List<AiringAnime> LeftOver { get; set; } = new List<AiringAnime>();
    }
}