using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using Newtonsoft.Json;
using static anime_downloader.Classes.ApiKeys;

namespace anime_downloader.Services
{
    // http://anilist-api.readthedocs.io/en/latest/anime.html#browse
    public class AniListApi: IAniListApi
    {
        private readonly ICredentialsRepository _credentialsRepository;

        private ApiCredentials _credentials;

        private const string Prefix = "https://anilist.co/api";

        private static string AnimeUrl(int id) => $"{Prefix}/anime/{id}";

        private static string FullAnimeUrl(int id) => $"{Prefix}/anime/{id}/page";

        private static string SearchUrl(string query) => $"{Prefix}/anime/search/{query}";

        private static string AuthUrl => $"{Prefix}/auth/access_token";

        private static string BrowseUrl => $"{Prefix}/browse/anime";

        // 

        public AniListApi(ICredentialsRepository credentialsRepository)
        {
            _credentialsRepository = credentialsRepository;
            _credentials = _credentialsRepository.AniListConfiguration.Credentials;
        }

        // 

        public async Task<List<AiringAnime>> GetNewAnimes(AnimeSeason season)
        {
            return await GetBrowse(await BuildBrowseUrl(season));
        }

        public async Task<List<AiringAnime>> GetLeftoverAnime(AnimeSeason season)
        {
            var data = await GetBrowse(await BuildLeftoverUrl(season));
            return data
                .Where(anime => anime.Type == "TV")
                .Where(anime => anime.Airing?.NextEpisode.HasValue == true &&
                                Methods.InRange(anime.Airing.NextEpisode.Value, 10, 24))
                .Where(anime => anime.TotalEpisodes > 12)
                .ToList();
        }

        public async Task<AiringAnime> GetAnime(int id, bool fullProfile)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var request = await client.GetAsync(await BuildAnimeUrl(id, fullProfile));
                    var response = await request.Content.ReadAsStringAsync();

                    return !response.Contains("\"error\"")
                        ? JsonConvert.DeserializeObject<AiringAnime>(response)
                        : null;
                }
            }

            catch
            {
                return null;
            }
        }

        public async Task<List<AiringAnime>> FindAnime(string q)
        {
            return await GetBrowse(await BuildSearchUrl(q));
        }

        // 

        /// <summary>
        ///     GET on the /browse/{} endpoint returning AiringAnime, for new/leftover
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

                    return new List<AiringAnime>();
                }
            }

            catch
            {
                return new List<AiringAnime>();
            }
        }
        
        // URL related

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

        /// <summary>
        ///     Build the url for gathering an airing anime profile
        /// </summary>
        private async Task<string> BuildAnimeUrl(int id, bool fullProfile = true)
        {
            await CheckAuthentication();

            var builder = new UriBuilder(fullProfile ? FullAnimeUrl(id) : AnimeUrl(id));
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_token"] = _credentials.AccessToken;
            builder.Query = query.ToString();
            return builder.ToString();
        }
        
        private async Task<string> BuildAnimeUrl(AiringAnimeSmall anime) => await BuildAnimeUrl(anime.Id);
        
        private async Task<string> BuildAnimeUrl(Anime anime) => await BuildAnimeUrl(anime.Details.AniId);

        private async Task<string> BuildSearchUrl(string q)
        {
            await CheckAuthentication();
            var builder = new UriBuilder(SearchUrl(HttpUtility.UrlEncode(q)));
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_token"] = _credentials.AccessToken;
            builder.Query = query.ToString();
            return builder.ToString();
        }

        // Auth related

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
}