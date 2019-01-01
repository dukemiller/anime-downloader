using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

        private static bool _collecting;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        
        // 

        public AniListService(IAniListApi api)
        {
            _api = api;
            _data = AniListData.Load();
        }

        // 
        
        public async Task<List<AiringAnime>> New(AnimeSeason animeSeason, Action startLoading)
        {
            return await _data.New(AnimeSeason.Current, async () => await _api.GetNewAnimes(animeSeason), startLoading);
        }

        public async Task<List<AiringAnime>> Leftover(AnimeSeason animeSeason, Action startLoading)
        {
            return await _data.Leftovers(AnimeSeason.Current, async () => await _api.GetLeftoverAnime(animeSeason), startLoading);
        }

        public async Task CollectResources(AiringAnime anime)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                while (_collecting)
                    await Task.Delay(10);

                _collecting = true;
                await _data.DownloadImage(anime).ConfigureAwait(false);
            }

            catch (Exception e)
            {
                Console.WriteLine();
                //
            }

            finally
            {
                _semaphoreSlim.Release();
            }

            _collecting = false;
        }
    }

    [Serializable]
    internal class AniListData
    {
        private readonly WebClient _downloader = new WebClient();

        private AniListData() { }

        [JsonIgnore]
        private static readonly string SettingsPath = Path.Combine(PathConfiguration.ApplicationDirectory, "airing_shows.json");
        
        [JsonProperty("data")]
        public Dictionary<string, AniListSeasonData> Data { get; set; } = new Dictionary<string, AniListSeasonData>();

        public async Task<List<AiringAnime>> New(AnimeSeason season, Func<Task<List<AiringAnime>>> getNew, Action startLoading)
        {
            if (!Data.ContainsKey(season.Title))
                Data[season.Title] = new AniListSeasonData();

            var data = Data[season.Title];
            
            // two weeks old or there are no entries
            if ((DateTime.Now - data.LastCheckedNew).TotalHours > 24 * 14 || data.New.Count == 0)
            {
                startLoading();
                Data[season.Title].New = await getNew();
                Data[season.Title].LastCheckedNew = DateTime.Now;
                await Save();
            }

            return Data[season.Title].New;
        }

        public async Task<List<AiringAnime>> Leftovers(AnimeSeason season, Func<Task<List<AiringAnime>>> getLeftovers, Action startLoading)
        {
            if (!Data.ContainsKey(season.Title))
                Data[season.Title] = new AniListSeasonData();

            var data = Data[season.Title];

            // two weeks old or there are no entries
            if ((DateTime.Now - data.LastCheckedLeftover).TotalHours > 24 * 14 || data.LeftOver.Count == 0)
            {
                startLoading();
                Data[season.Title].LeftOver = await getLeftovers();
                Data[season.Title].LastCheckedLeftover = DateTime.Now;
                await Save();
            }

            return Data[season.Title].LeftOver;
        }

        public async Task DownloadImage(AiringAnime anime)
        {
            var changed = false;
            var image = anime.CoverImage.Large;
            var downloadPath = Path.Combine(SettingsRepository.ImageDirectory, $"{anime.Id}.png");

            if (File.Exists(downloadPath))
            {
                var length = new FileInfo(downloadPath).Length;
                if (length < 8)
                    File.Delete(downloadPath);
            }

            if (!File.Exists(downloadPath))
                await _downloader.DownloadFileTaskAsync(image, downloadPath).ConfigureAwait(false);
            
            if (image != downloadPath)
            {
                anime.CoverImage.Large = downloadPath;
                changed = true;
            }

            if (changed)
                await Save();
        }

        public static AniListData Load()
        {
            if (!Directory.Exists(SettingsRepository.ImageDirectory))
                Directory.CreateDirectory(SettingsRepository.ImageDirectory);

            // don't care about re-initializing, not user centered data
            try
            {
                if (File.Exists(SettingsPath))
                    using (var stream = new StreamReader(SettingsPath))
                        return JsonConvert.DeserializeObject<AniListData>(stream.ReadToEnd());
            }

            catch
            {

            }

            return new AniListData();
        }

        public async Task Save()
        {
            using (var stream = new StreamWriter(SettingsPath))
                await stream.WriteAsync(JsonConvert.SerializeObject(this, Formatting.Indented,
                    new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore}));
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