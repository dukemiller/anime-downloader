using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace anime_downloader.Services
{
    /// <summary>
    ///     An implementation using the sites internal api for how it handles changes on regular pages
    /// </summary>
    public class MyAnimeListInternalApi : IMyAnimeListApi
    {
        private readonly ICredentialsRepository _credentialsRepository;

        private const string ApiAdd = "https://myanimelist.net/ownlist/anime/add.json";

        private const string ApiUpdate = "https://myanimelist.net/ownlist/anime/edit.json";

        private const string ApiUpdateDetailed = "https://myanimelist.net/editlist.php?type=anime&id={0}";

        private const string ApiSearch = "https://myanimelist.net/search/prefix.json?type=anime&keyword={0}&v=1";

        private const string UrlLogin = "https://myanimelist.net/login.php?from=%2F";
        
        private const string ApiVerify = "https://myanimelist.net/api/account/verify_credentials.xml";

        private const string ApiProfile = "https://myanimelist.net/malappinfo.php?u={0}&status=all&type=anime";
        
        private static readonly XmlSerializer ProfileDeserializer = new XmlSerializer(typeof(ProfileResult));

        private readonly HttpClient _client;

        private readonly ApiCredentials _credentials;

        private bool _clientReady;

        // 

        public MyAnimeListInternalApi(ICredentialsRepository credentialsRepository)
        {
            _credentialsRepository = credentialsRepository;

            // Remove any api details on any changes to login credentials
            _credentialsRepository.MyAnimeListConfig.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "Username" || args.PropertyName == "Password")
                {
                    _credentialsRepository.MyAnimeListConfig.Credentials = new ApiCredentials();
                    _credentialsRepository.Save();
                    _clientReady = false;
                }
            };

            _client = new HttpClient(new HttpClientHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            _credentials = _credentialsRepository.MyAnimeListConfig.Credentials;
        }

        // 

        public async Task<bool> VerifyCredentialsAsync()
        {
            using (var client = new HttpClient(new HttpClientHandler { Credentials = GetCredentials() }))
            {
                var response = await client.GetAsync(ApiVerify);
                return response.StatusCode == HttpStatusCode.OK;
            }
        }

        public async Task<IEnumerable<ProfileAnimeResult>> GetProfile()
        {
            var url = string.Format(ApiProfile, _credentialsRepository.MyAnimeListConfig.Username);
            using (var client = new HttpClient())
            {
                var request = (await client.GetAsync(url)).Content;
                var data = await request.ReadAsStreamAsync();
                if (data == null || data.Length <= 0)
                    return new List<ProfileAnimeResult>();
                using (var response = new StreamReader(data))
                {
                    var result = (ProfileResult)ProfileDeserializer.Deserialize(response);
                    return result.Anime.Where(anime =>
                    {
                        var withinLastThreeYears = DateTime.TryParse(anime?.SeriesStart, out DateTime date) &&
                                                   Math.Abs(DateTime.Now.Year - date.Year) <= 3;
                        var isShortOrSeries = anime?.SeriesType?.Equals("1") == true ||
                                              anime?.SeriesType?.Equals("2") == true;
                        var definitelyNotAnOva = int.Parse(anime?.SeriesEpisodes ?? "0") > 4;
                        return withinLastThreeYears && isShortOrSeries && definitelyNotAnOva;
                    });
                }
            }
        }

        public async Task<IEnumerable<FindResult>> FindAsync(string q)
        {
            q = HttpUtility.UrlEncode(q)?.Replace("%20", "%25");
            var url = string.Format(ApiSearch, q);
            var data = (await _client.GetAsync(url)).Content;
            var json = await data.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<SearchResponse>(json);
            return response.Categories.FirstOrDefault()?.Items.Select(ToFindResult) ?? new List<FindResult>();
        }

        public async Task<(bool successful, string content)> AddAsync(Anime anime)
        {
            await SetupRequest();

            var response = await Post(ApiAdd, anime);
            var content = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, content);
        }

        public async Task<(bool successful, string content)> UpdateAsync(Anime anime)
        {
            await SetupRequest();

            // Adding tags requires a whole different endpoint
            var url = anime.Notes?.Length > 0
                ? string.Format(ApiUpdateDetailed, anime.Details.Id)
                : ApiUpdate;
            var response = await Post(url, anime);
            var content = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, content);
        }

        // 

        private readonly HttpStatusCode[] _errorCodes = 
        {
            HttpStatusCode.InternalServerError, HttpStatusCode.GatewayTimeout,
            HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway,
            HttpStatusCode.BadRequest
        };

        private async Task<HttpResponseMessage> Post(string url, Anime anime)
        {
            var response = await _client.PostAsync(url, await GetPayload(anime));

            // Error 400 usually in this context means that the session/cookies/csrf are invalid,
            // probably because you logged on the site on your own at some point. In the future,
            // get a more concrete idea on why this happens
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                await Login();
                await SetupClient();
                response = await _client.PostAsync(url, await GetPayload(anime));
            }

            // If I still get some sort of error, throw it and stop doing any further requests
            foreach (var code in _errorCodes)
                if (response.StatusCode == code)
                    throw new ServerProblemException(code);

            return response;
        }

        private NetworkCredential GetCredentials() => new NetworkCredential(_credentialsRepository.MyAnimeListConfig.Username, _credentialsRepository.MyAnimeListConfig.Password);
        
        private async Task SetupRequest()
        {
            if (!_clientReady)
                await SetupClient();

            if (_credentials.NeedNewToken)
                await RetrieveCsrf();
        }

        private async Task SetupClient()
        {
            if (string.IsNullOrEmpty(_credentials.Cookies))
                await Login();

            _client.DefaultRequestHeaders.Clear();

            _client.DefaultRequestHeaders.Host = "myanimelist.net";
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0) Gecko/20100101 Firefox/55.0");
            _client.DefaultRequestHeaders.Add("Accept", "*/*");
            _client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            _client.DefaultRequestHeaders.Add("DNT", "1");
            _client.DefaultRequestHeaders.Add("Cookie", _credentials.Cookies);
            _client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            _clientReady = true;
        }

        private async Task Login()
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler {CookieContainer = cookieContainer};
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0) Gecko/20100101 Firefox/55.0");

                // Get the csrf_token

                var page = await client.GetAsync(UrlLogin);
                var document = new HtmlDocument();
                document.LoadHtml(await page.Content.ReadAsStringAsync());
                var csrf = document.DocumentNode
                    .Descendants("meta")
                    .First(node => node.Attributes["name"]?.Value == "csrf_token")
                    .Attributes["content"].Value;

                // Create the request

                var pairs = new Dictionary<string, string>
                {
                    {"user_name", _credentialsRepository.MyAnimeListConfig.Username},
                    {"password", _credentialsRepository.MyAnimeListConfig.Password},
                    {"cookie", "1"},
                    {"sublogin", "Login"},
                    {"submit", "1"},
                    {"csrf_token", csrf}
                };

                var content = new FormUrlEncodedContent(pairs);
                var request = await client.PostAsync(UrlLogin, content);
                var cookies = string.Join("; ",
                    cookieContainer.GetCookies(new Uri(UrlLogin))
                        .Cast<Cookie>()
                        .Select(cookie => cookie.ToString()));

                // Save information to disk

                _credentials.CsrfToken = csrf;
                _credentials.CsrfTokenLastRetrieved = DateTime.Now;
                _credentials.Cookies = cookies;

                _credentialsRepository.Save();
            }
        }

        private async Task RetrieveCsrf()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:55.0) Gecko/20100101 Firefox/55.0");

                // Get the csrf_token

                var page = await client.GetAsync(UrlLogin);
                var document = new HtmlDocument();
                document.LoadHtml(await page.Content.ReadAsStringAsync());
                var csrf = document.DocumentNode
                    .Descendants("meta")
                    .First(node => node.Attributes["name"]?.Value == "csrf_token")
                    .Attributes["content"].Value;

                // Save information to disk

                _credentials.CsrfToken = csrf;
                _credentials.CsrfTokenLastRetrieved = DateTime.Now;

                _credentialsRepository.Save();
            }
        }

        private async Task<HttpContent> GetPayload(Anime anime)
        {
            return anime.Notes?.Length > 0
                ? new FormUrlEncodedContent(ToUpdatePairs(anime, _credentials.CsrfToken))
                : new StringContent(ToShowRequestJson(anime, _credentials.CsrfToken), Encoding.UTF8, "application/json") as HttpContent;
        }

        // 

        private static FindResult ToFindResult(Item item)
        {
            string start, end;
            var date = item.Payload.Aired;

            if (date.Contains(" to "))
            {
                var data = date.Split(new[] { " to " }, StringSplitOptions.None);
                start = data[0];
                end = data[1];
            }
            else
            {
                start = date;
                end = "";
            }

            var result = new FindResult
            {
                Id = item.Id.ToString(),
                Score = item.Payload.Score,
                Type = item.Payload.MediaType,
                Status = item.Payload.Status,
                Title = item.Name,
                Image = item.ImageUrl,
                StartDate = start,
                EndDate = "",
                TotalEpisodes = 0,
                Synopsis = "",
                English = "",
                Synonyms = ""
            };

            if (!string.IsNullOrEmpty(end) && end != "?")
                result.EndDate = end;

            return result;
        }

        private static string ToShowRequestJson(Anime anime, string csrf)
        {
            var episode = anime.SeriesContinuationEpisode != 0
                ? Math.Abs(anime.SeriesContinuationEpisode)
                : anime.Episode;

            var rating = !string.IsNullOrEmpty(anime.Rating)
                ? int.Parse(anime.Rating)
                : 0;

            int status;

            switch (anime.Status)
            {
                case Status.Watching:
                    status = 1;
                    break;
                case Status.Considering:
                    status = 6;
                    break;
                case Status.Finished:
                    status = 2;
                    break;
                case Status.OnHold:
                    status = 3;
                    break;
                case Status.Dropped:
                    status = 4;
                    break;
                default:
                    status = 6;
                    break;
            }

            var request = new ShowRequest
            {
                Id = int.Parse(anime.Details.Id),
                Episodes = episode,
                Score = rating,
                Status = status,
                CsrfToken = csrf
            };

            return JsonConvert.SerializeObject(request);
        }

        private static Dictionary<string, string> ToUpdatePairs(Anime anime, string csrf)
        {
            var episode = anime.SeriesContinuationEpisode != 0
                ? anime.SeriesContinuationEpisode.ToString()
                : anime.Episode.ToString();
            episode = episode.Replace("-", "");

            var rating = !string.IsNullOrEmpty(anime.Rating)
                ? int.Parse(anime.Rating)
                : 0;

            string status;

            switch (anime.Status)
            {
                case Status.Watching:
                    status = "1";
                    break;
                case Status.Considering:
                    status = "6";
                    break;
                case Status.Finished:
                    status = "2";
                    break;
                case Status.OnHold:
                    status = "3";
                    break;
                case Status.Dropped:
                    status = "4";
                    break;
                default:
                    status = "6";
                    break;
            }

            // Potential error here:
            // - "astatus" refers to the myanimelist reference for the shows status
            // - "add_anime[status]" refers to the users status
            // why I have to include what they consider the status of their show in the request
            // i wont ever know

            return new Dictionary<string, string>
            {
                {"anime_id", anime.Details.Id},
                {"aeps", anime.Details.TotalEpisodes.ToString()},
                {"astatus", status},
                {"add_anime[status]", status},
                {"add_anime[num_watched_episodes]", episode},
                {"add_anime[score]", rating.ToString()},
                {"add_anime[start_date][month]", ""},
                {"add_anime[start_date][day]", ""},
                {"add_anime[start_date][year]", ""},
                {"add_anime[finish_date][month]", ""},
                {"add_anime[finish_date][day]", ""},
                {"add_anime[finish_date][year]", ""},
                {"add_anime[tags]", anime.Notes},
                {"add_anime[priority]", "0"},
                {"add_anime[storage_type]", ""},
                {"add_anime[storage_value]", "0"},
                {"add_anime[num_watched_times]", "0"},
                {"add_anime[rewatch_value]", ""},
                {"add_anime[comments]", ""},
                {"add_anime[is_asked_to_discuss]", "0"},
                {"add_anime[sns_post_type]", "0"},
                {"submitIt", "0"},
                {"csrf_token", csrf}
            };
        }
    }

}