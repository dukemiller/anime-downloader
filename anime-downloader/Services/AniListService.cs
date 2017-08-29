using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using Newtonsoft.Json;
using static anime_downloader.Classes.ApiKeys;

namespace anime_downloader.Services
{
    // http://anilist-api.readthedocs.io/en/latest/anime.html#browse
    public class AniListService : IFindSeasonAnimeService
    {
        private readonly ICredentialsRepository _credentialsRepository;

        private ApiCredentials _credentials;

        private const string Prefix = "https://anilist.co/api";

        private static string AnimeUrl(int id) => $"{Prefix}/anime/{id}/page";

        private static string AuthUrl => $"{Prefix}/auth/access_token";

        private static string BrowseUrl => $"{Prefix}/browse/anime";
        
        private readonly AniListData _data;

        // 

        public AniListService(ICredentialsRepository credentialsRepository)
        {
            _credentialsRepository = credentialsRepository;
            _credentials = _credentialsRepository.AniListConfiguration.Credentials;
            _data = AniListData.Load();
        }

        // 
        
        public async Task<List<AiringAnime>> New(AnimeSeason animeSeason)
        {
            return await _data.New(
                AnimeSeason.Current, 
                async () => await GetBrowse(await BuildBrowseUrl(animeSeason)));
        }

        public async Task<List<AiringAnime>> Leftover(AnimeSeason animeSeason)
        {
            return await _data.Leftovers(
                AnimeSeason.Current,
                async () =>
                {
                    var data = await GetBrowse(await BuildLeftoverUrl(animeSeason));
                    return data
                        .Where(anime => anime.Type == "TV")
                        .Where(anime => anime.Airing?.NextEpisode.HasValue == true &&
                                        Methods.InRange(anime.Airing.NextEpisode.Value, 10, 24))
                        .Where(anime => anime.TotalEpisodes > 12)
                        .ToList();
                }
            );
        }

        public async Task FillInDetails(AnimeSeason season, bool isNew, AiringAnime anime)
        {
            await CheckAuthentication();

            anime.Description = "Loading ...";

            try
            {
                using (var client = new HttpClient())
                {
                    var request = await client.GetAsync(await BuildAnimeUrl(anime));
                    var response = await request.Content.ReadAsStringAsync();

                    if (!response.Contains("\"error\""))
                    {
                        var updatedAnime = JsonConvert.DeserializeObject<AiringAnime>(response);
                        await _data.UpdateEntry(season, isNew, updatedAnime);

                        UpdateObservableProperties(anime, updatedAnime);
                    }

                    else
                    {
                        anime.Description = "";
                    }
                }
            }

            catch
            {
                // ignored
            }
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
        
        /// <summary>
        ///     GET on the /browse/{} endpoint returning AiringAnime
        /// </summary>
        private static async Task<List<AiringAnime>> GetBrowse(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var request = await client.GetAsync(url);
                    var response = await request.Content.ReadAsStringAsync();

                    if (!response.Contains("\"error\""))
                    {
                        var animes = JsonConvert
                            .DeserializeObject<List<AiringAnime>>(response)
                            .Where(anime => anime.Type.Contains("TV"))
                            .ToList();
                        
                        return animes;
                    }

                    Console.WriteLine(response);

                    return new List<AiringAnime>();
                }
            }

            catch
            {
                return new List<AiringAnime>();
            }
        }

        private async Task CheckAuthentication()
        {
            ApiCredentials credentials = null;

            // never retrieved: retrieve
            if (_credentials == null)
                credentials = await GetCredentials();

            // token expired: update
            else if (_credentials?.ExpiresDateTime < DateTime.Now)
                credentials = await GetCredentials();

            // a change was required to be made, save new credentials to settings
            if (credentials != null)
            {
                _credentials = credentials;
                _credentialsRepository.AniListConfiguration.Credentials = credentials;
                _credentialsRepository.Save();
            }
        }

        /// <summary>
        ///     Build the url for gathering general anime of this season
        /// </summary>
        private async Task<string> BuildBrowseUrl(AnimeSeason animeSeason)
        {
            await CheckAuthentication();

            var builder = new UriBuilder(BrowseUrl);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_token"] = _credentials.AccessToken;
            query["year"] = animeSeason.Year.ToString();
            query["season"] = animeSeason.Season.Description();
            query["sort"] = "popularity-desc";
            query["full_page"] = "true";
            builder.Query = query.ToString();
            return builder.ToString();
        }

        /// <summary>
        ///     Build the url for gathering anime that are leftover from the season previous to the current
        /// </summary>
        private async Task<string> BuildLeftoverUrl(AnimeSeason animeSeason)
        {
            await CheckAuthentication();

            var builder = new UriBuilder(BrowseUrl);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_token"] = _credentials.AccessToken;
            query["airing_data"] = "true";
            query["status"] = "Currently Airing";
            query["year"] = animeSeason.Previous().Year.ToString();
            query["season"] = animeSeason.Previous().Season.Description();
            query["sort"] = "popularity-desc";
            query["full_page"] = "true";
            builder.Query = query.ToString();
            return builder.ToString();
        }

        private async Task<string> BuildAnimeUrl(AiringAnimeSmall anime)
        {
            await CheckAuthentication();

            var builder = new UriBuilder(AnimeUrl(anime.Id));
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_token"] = _credentials.AccessToken;
            builder.Query = query.ToString();
            return builder.ToString();
        }

        /// <summary>
        ///     Authenticate to the API server
        /// </summary>
        private static async Task<ApiCredentials> GetCredentials()
        {
            using (var client = new HttpClient())
            {
                var pairs = new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", AniListId},
                    {"client_secret", AniListSecret}
                };
                var content = new FormUrlEncodedContent(pairs);
                var request = await client.PostAsync(AuthUrl, content);
                var response = await request.Content.ReadAsStringAsync();
                var credentials = JsonConvert.DeserializeObject<ApiCredentials>(response);
                return credentials;
            }
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