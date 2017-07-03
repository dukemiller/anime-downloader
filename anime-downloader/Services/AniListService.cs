using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Services.Interfaces;
using Newtonsoft.Json;
using static anime_downloader.Classes.ApiKeys;

namespace anime_downloader.Services
{
    // http://anilist-api.readthedocs.io/en/latest/anime.html#browse
    public class AniListService : IFindSeasonAnimeService
    {
        private readonly ISettingsService _settings;

        private const string Prefix = "https://anilist.co/api";

        private static string AuthUrl => $"{Prefix}/auth/access_token";

        private static string BrowseUrl => $"{Prefix}/browse/anime";

        private DateTime _lastChecked = DateTime.Now.AddMinutes(-60);

        private ClientCredentials _credentials;

        private IEnumerable<AiringAnime> _lastResultsNew;

        private IEnumerable<AiringAnime> _lastResultsLeftover;

        // 

        public AniListService(ISettingsService settings)
        {
            _settings = settings;
            _credentials = settings.AniListConfiguration.Credentials;
        }

        // 


        public async Task<IEnumerable<AiringAnime>> New(AnimeSeason animeSeason)
        {
            // If never initially gathered or 60 minutes pass check, otherwise return last found results
            if (_lastResultsNew == null || DateTime.Now >= _lastChecked.AddMinutes(60))
            {
                var current = await GetBrowse(BuildBrowseUrl(_credentials, animeSeason));
                _lastResultsNew = current;
                _lastChecked = DateTime.Now;
            }

            return _lastResultsNew;
        }

        public async Task<IEnumerable<AiringAnime>> Leftover(AnimeSeason animeSeason)
        {
            // If never initially gathered or 60 minutes pass check, otherwise return last found results
            if (_lastResultsLeftover == null || DateTime.Now >= _lastChecked.AddMinutes(60))
            {
                var leftovers = (await GetBrowse(BuildLeftoverUrl(_credentials, animeSeason)))
                    .Where(anime => anime.Type == "TV")
                    .Where(anime => anime.Airing.NextEpisode.HasValue &&
                                    Methods.InRange(anime.Airing.NextEpisode.Value, 10, 24))
                    .Where(anime => anime.TotalEpisodes > 12);

                _lastResultsLeftover = leftovers;
                _lastChecked = DateTime.Now;
            }

            return _lastResultsLeftover;
        }
        
        /// <summary>
        ///     GET on the /browse/{} endpoint returning AiringAnime
        /// </summary>
        private async Task<IEnumerable<AiringAnime>> GetBrowse(string url)
        {
            await CheckAuthentication();

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
                            .Where(anime => anime.Type.Contains("TV"));


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
            ClientCredentials credentials = null;

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
                _settings.AniListConfiguration.Credentials = credentials;
                _settings.Save();
            }
        }

        /// <summary>
        ///     Build the url for gathering general anime of this season
        /// </summary>
        private static string BuildBrowseUrl(ClientCredentials credentials, AnimeSeason animeSeason)
        {
            var builder = new UriBuilder(BrowseUrl);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_token"] = credentials.AccessToken;
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
        private static string BuildLeftoverUrl(ClientCredentials credentials, AnimeSeason animeSeason)
        {
            var builder = new UriBuilder(BrowseUrl);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_token"] = credentials.AccessToken;
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
        ///     Authenticate to the API server
        /// </summary>
        /// <returns></returns>
        private static async Task<ClientCredentials> GetCredentials()
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
                var credentials = JsonConvert.DeserializeObject<ClientCredentials>(response);
                return credentials;
            }
        }

    }
}