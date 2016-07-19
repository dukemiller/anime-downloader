using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace anime_downloader.Classes.Web.MyAnimeList
{
    public static class Api
    {
        // http://myanimelist.net/modules.php?go=api

        private const string ApiSearch = "http://myanimelist.net/api/anime/search.xml?q={0}";

        private const string ApiAdd = "http://myanimelist.net/api/animelist/add/{0}.xml";

        private const string ApiUpdate = "http://myanimelist.net/api/animelist/update/{0}.xml";

        private const string ApiDelete = "http://myanimelist.net/api/animelist/delete/{0}.xml";

        private const string ApiVerify = "http://myanimelist.net/api/account/verify_credentials.xml";

        private static readonly XmlSerializer ResultDeserializer = new XmlSerializer(typeof(FindResultRoot));

        private static async Task<HttpContent> GetAsync(ICredentials credentials, string url)
        {
            var handler = new HttpClientHandler { Credentials = credentials };
            var client = new HttpClient(handler);
            var response = (await client.GetAsync(url)).Content;
            return response;
        }

        private static async Task<HttpContent> PostAsync(ICredentials credentials, string url, string data)
        {
            var handler = new HttpClientHandler { Credentials = credentials };
            var client = new HttpClient(handler);
            var pairs = new Dictionary<string, string>
            {
                {"data", data}
            };
            var content = new FormUrlEncodedContent(pairs);
            var response = (await client.PostAsync(url, content)).Content;
            return response;
        }

        public static NetworkCredential GetCredentials(Settings settings)
        {
            return new NetworkCredential(settings.MyAnimeList.Username, settings.MyAnimeList.Password);
        }

        public static async Task<bool> VerifyAsync(ICredentials credentials)
        {
            const string url = ApiVerify;
            var handler = new HttpClientHandler { Credentials = credentials };
            var client = new HttpClient(handler);
            var response = await client.GetAsync(url);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public static async Task<List<FindResult>> FindAsync(ICredentials credentials, string q)
        {
            var url = string.Format(ApiSearch, q);
            var request = await GetAsync(credentials, url);
            var data = await request.ReadAsStreamAsync();
            if (data == null || data.Length <= 0)
                return new List<FindResult>();
            using (var response = new StreamReader(data))
            {
                var result = (FindResultRoot) ResultDeserializer.Deserialize(response);
                return result.Entries;
            }
        }

        public static async Task<HttpContent> AddAsync(ICredentials credentials, string id, string data)
        {
            var url = string.Format(ApiAdd, id);
            var response = await PostAsync(credentials, url, data);
            return response;
        }

        public static async Task<HttpContent> UpdateAsync(ICredentials credentials, string id, string data)
        {
            var url = string.Format(ApiUpdate, id);
            var response = await PostAsync(credentials, url, data);
            return response;
        }

        public static async Task<HttpContent> DeleteAsync(ICredentials credentials, string id, string data)
        {
            var url = string.Format(ApiDelete, id);
            var response = await PostAsync(credentials, url, data);
            return response;
        }
    }
}