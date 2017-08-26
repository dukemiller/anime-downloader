using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    // http://myanimelist.net/modules.php?go=api
    public class MyAnimeListApi: IMyAnimeListApi
    {
        private readonly ISettingsService _settings;

        private const string ApiSearch = "https://myanimelist.net/api/anime/search.xml?q={0}";

        private const string ApiAdd = "https://myanimelist.net/api/animelist/add/{0}.xml";

        private const string ApiUpdate = "https://myanimelist.net/api/animelist/update/{0}.xml";
        
        private const string ApiVerify = "https://myanimelist.net/api/account/verify_credentials.xml";

        private const string ApiProfile = "https://myanimelist.net/malappinfo.php?u={0}&status=all&type=anime";

        private static readonly XmlSerializer ResultDeserializer = new XmlSerializer(typeof(FindResultRoot));

        private static readonly XmlSerializer ProfileDeserializer = new XmlSerializer(typeof(ProfileResult));

        private HttpClient _client;

        public MyAnimeListApi(ISettingsService settings)
        {
            _settings = settings;
            _settings.MyAnimeListConfig.PropertyChanged += (sender, args) =>
            {
                _client = new HttpClient(new HttpClientHandler { Credentials = GetCredentials() });
            };
            _client = new HttpClient(new HttpClientHandler { Credentials = GetCredentials() });
        }

        // 

        private NetworkCredential GetCredentials() => new NetworkCredential(_settings.MyAnimeListConfig.Username, _settings.MyAnimeListConfig.Password);

        public async Task<bool> VerifyCredentialsAsync()
        {
            const string url = ApiVerify;
            var response = await _client.GetAsync(url);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<IEnumerable<ProfileAnimeResult>> GetProfile()
        {
            await VerificationCheck();

            var url = string.Format(ApiProfile, _settings.MyAnimeListConfig.Username);
            var request = await GetAsync(url);
            var data = await request.ReadAsStreamAsync();
            if (data == null || data.Length <= 0)
                return new List<ProfileAnimeResult>();
            using (var response = new StreamReader(data))
            {
                var result = (ProfileResult)ProfileDeserializer.Deserialize(response);
                return result.Anime.Where(anime =>
                {
                    var withinLastThreeYears = DateTime.TryParse(anime?.SeriesStart, out DateTime date) && Math.Abs(DateTime.Now.Year - date.Year) <= 3;
                    var isShortOrSeries = anime?.SeriesType?.Equals("1") == true || anime?.SeriesType?.Equals("2") == true;
                    var definitelyNotAnOva = int.Parse(anime?.SeriesEpisodes ?? "0") > 4;
                    return withinLastThreeYears && isShortOrSeries && definitelyNotAnOva;
                });
            }
        }

        public async Task<IEnumerable<FindResult>> FindAsync(string q)
        {
            await VerificationCheck();

            q = HttpUtility.UrlPathEncode(q).Replace("%20", "%25");
            var url = string.Format(ApiSearch, q);
            var request = await GetAsync(url);
            var data = await request.ReadAsStreamAsync();
            if (data == null || data.Length <= 0)
                return new List<FindResult>();
            using (var response = new StreamReader(data))
            {
                var result = (FindResultRoot)ResultDeserializer.Deserialize(response);
                return result.Entries.Where(anime =>
                {
                    return (!anime.Type.Equals("Movie") 
                    && !anime.Type.Equals("OVA") 
                    && (anime.TotalEpisodes == 0 || anime.TotalEpisodes > 4)) 
                    // || anime.Type.Equals("Special")
                    ;
                });
            }
        }

        public async Task<HttpContent> AddAsync(Anime anime)
        {
            var episode = anime.MyAnimeList.SeriesContinuationEpisode != null
                ? int.Parse(anime.MyAnimeList.SeriesContinuationEpisode)
                : anime.Episode;
            var data = new UpdateShow(anime, episode).ToString();
            var url = string.Format(ApiAdd, anime.MyAnimeList.Id);
            var response = await PostAsync(url, data);
            return response;
        }

        public async Task<HttpContent> UpdateAsync(Anime anime)
        {
            var episode = anime.MyAnimeList.SeriesContinuationEpisode != null
                ? int.Parse(anime.MyAnimeList.SeriesContinuationEpisode)
                : anime.Episode;
            var data = new UpdateShow(anime, episode).ToString();
            var url = string.Format(ApiUpdate, anime.MyAnimeList.Id);
            var response = await PostAsync(url, data);
            return response;
        }

        // 

        private async Task<HttpContent> GetAsync(string url)
        {
            await VerificationCheck();

            var response = (await _client.GetAsync(url)).Content;
            return response;
        }

        private async Task<HttpContent> PostAsync(string url, string data)
        {
            await VerificationCheck();

            var pairs = new Dictionary<string, string>
            {
                {"data", data}
            };
            var content = new FormUrlEncodedContent(pairs);
            var response = (await _client.PostAsync(url, content)).Content;
            return response;
        }
        
        public bool IsVerified { get; set; }

        private async Task VerificationCheck()
        {
            if (!IsVerified)
            {
                IsVerified = await VerifyCredentialsAsync();
                if (!IsVerified)
                    throw new MyAnimeListCredentialsException();
            }
        }
    }

    public class MyAnimeListCredentialsException : Exception
    {
        
    }
}
