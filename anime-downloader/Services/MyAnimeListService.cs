using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Services
{
    // http://myanimelist.net/modules.php?go=api
    public class MyAnimeListService: IMyAnimeListService
    {
        private ISettingsService Settings { get; set; }

        private IAnimeService Anime { get; set; }

        private const string ApiSearch = "https://myanimelist.net/api/anime/search.xml?q={0}";

        private const string ApiAdd = "https://myanimelist.net/api/animelist/add/{0}.xml";

        private const string ApiUpdate = "https://myanimelist.net/api/animelist/update/{0}.xml";

        private const string ApiDelete = "https://myanimelist.net/api/animelist/delete/{0}.xml";

        private const string ApiVerify = "https://myanimelist.net/api/account/verify_credentials.xml";

        // 

        public MyAnimeListService(ISettingsService settings, IAnimeService anime)
        {
            Settings = settings;
            Anime = anime;
        }

        //

        private static readonly XmlSerializer ResultDeserializer = new XmlSerializer(typeof(FindResultRoot));

        private async Task<HttpContent> GetAsync(string url)
        {
            var handler = new HttpClientHandler { Credentials = GetCredentials() };
            var client = new HttpClient(handler);
            var response = (await client.GetAsync(url)).Content;
            return response;
        }

        private async Task<HttpContent> PostAsync(string url, string data)
        {
            var handler = new HttpClientHandler { Credentials = GetCredentials() };
            var client = new HttpClient(handler);
            var pairs = new Dictionary<string, string>
            {
                {"data", data}
            };
            var content = new FormUrlEncodedContent(pairs);
            var response = (await client.PostAsync(url, content)).Content;
            return response;
        }

        private async Task<List<FindResult>> FindAsync(string q)
        {
            var url = string.Format(ApiSearch, q);
            var request = await GetAsync(url);
            var data = await request.ReadAsStreamAsync();
            if (data == null || data.Length <= 0)
                return new List<FindResult>();
            using (var response = new StreamReader(data))
            {
                var result = (FindResultRoot)ResultDeserializer.Deserialize(response);
                return result.Entries;
            }
        }

        private async Task<HttpContent> AddAsync(string id, string data)
        {
            var url = string.Format(ApiAdd, id);
            var response = await PostAsync(url, data);
            return response;
        }

        private async Task<HttpContent> UpdateAsync(string id, string data)
        {
            var url = string.Format(ApiUpdate, id);
            var response = await PostAsync(url, data);
            return response;
        }

        private async Task<HttpContent> DeleteAsync(string id, string data)
        {
            var url = string.Format(ApiDelete, id);
            var response = await PostAsync(url, data);
            return response;
        }

        // 

        public NetworkCredential GetCredentials() => new NetworkCredential(Settings.MyAnimeListConfig.Username, Settings.MyAnimeListConfig.Password);

        public async Task<bool> VerifyCredentialsAsync()
        {
            const string url = ApiVerify;
            var handler = new HttpClientHandler { Credentials = GetCredentials() };
            var client = new HttpClient(handler);
            var response = await client.GetAsync(url);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task Update(Anime anime)
        {
            var myAnimeListNode = !anime.MyAnimeList.SeriesContinuationEpisode.IsBlank()
                ? new UpdateShow(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                : new UpdateShow(anime);
            await UpdateAsync(anime.MyAnimeList.Id, myAnimeListNode.ToString());
            anime.MyAnimeList.NeedsUpdating = false;
        }

        public async Task Add(Anime anime)
        {
            var myAnimeListNode = !anime.MyAnimeList.SeriesContinuationEpisode.IsBlank()
                ? new UpdateShow(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                : new UpdateShow(anime);
            await AddAsync(anime.MyAnimeList.Id, myAnimeListNode.ToString());
            anime.MyAnimeList.NeedsUpdating = false;
        }

        public async Task<bool> GetId(Anime anime)
        {
            // get all results from searching the name
            var animeResults = await FindAsync(HttpUtility.UrlEncode(anime.Title.Replace(":", "")));

            // if there were absolutely no results from the query
            if (!animeResults.Any())
            {
                // Continually segment words and attempt to get a result
                var name = anime.Title.Split(' ');
                var length = name.Length;
                while (!animeResults.Any() && length-- > 1)
                {
                    var newName = string.Join(" ", name.Take(length));
                    animeResults = await FindAsync(HttpUtility.UrlEncode(newName));
                }

                // if after the previous operation there are still no results
                if (!animeResults.Any())
                {
                    // throw an error then skip
                    Methods.Alert($"1. Absolutely no matching names found for {anime.Title}.");
                    return false;
                }
            }

            // make an estimation as to what is the closest result related to the anime
            var result = anime.ClosestMyAnimeListResult(animeResults);

            // if there was no good guess
            if (result == null)
            {
                // try slapping a (TV) infront of it because the MAL api is weird sometimes
                animeResults = await FindAsync(HttpUtility.UrlEncode(anime.Title + " (TV)"));
                result = anime.ClosestMyAnimeListResult(animeResults);

                // if still no result
                if (result == null)
                {
                    // throw an error then skip
                    Methods.Alert($"2. No partial matches found from matching names for {anime.Title}.");
                    return false;
                }
            }

            // check episode details if there is a given total (you can only hope)
            if (result.IntTotalEpisodes() > 0)
            {
                // if you have downloaded more episodes than exists in the show, then you probably mislabeled
                // this show as a s2 show but i'll go through painstaking effort to make it work anyway
                if (anime.IntEpisode() > result.IntTotalEpisodes())
                {
                    // track episode total
                    var total = result.IntTotalEpisodes();

                    // remove current series from list of possible choices
                    animeResults.Remove(result);
                    result = anime.ClosestMyAnimeListResult(animeResults);
                    total += result?.IntTotalEpisodes() ?? 0;

                    // if the combination of both this season is still less than your current episode
                    // you've probably mislabeled this show for a few seasons dude, there's no way i can
                    // accurately guess which series is yours so i'll continue going through results until
                    // hopefully i can reach a point that it isnt
                    while (result != null && total < anime.IntEpisode())
                    {
                        animeResults.Remove(result);
                        result = anime.ClosestMyAnimeListResult(animeResults);
                        total += result?.IntTotalEpisodes() ?? 0;
                    }

                    // if we've run out of episodes, games over
                    if (result == null)
                    {
                        Methods.Alert($"3. Episode mismatch and no new series match for {anime.Title}.\n" +
                              $"Given total: {total}, current episode: {anime.IntEpisode()}");
                        return false;
                    }

                    // keep track of episodes to update instead in this variable
                    anime.MyAnimeList.SeriesContinuationEpisode = $"{anime.IntEpisode() - total:D2}";
                    anime.MyAnimeList.OverallTotal = $"{total:D2}";
                }

            }

            // add all the details available
            anime.MyAnimeList.Id = result.Id;
            anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;
            anime.MyAnimeList.Synopsis = result.Synopsis;
            anime.MyAnimeList.Image = result.Image;
            anime.MyAnimeList.Title = result.Title;
            anime.MyAnimeList.English = result.English;
            anime.MyAnimeList.Synonyms = result.Synonyms;

            return true;
        }

        public async Task Synchronize()
        {
            // for every anime that needs updating
            foreach (var anime in Anime.NeedsUpdates)
            {
                // if there is no id, get the id and add it
                if (anime.MyAnimeList.Id.IsBlank())
                {
                    if (await GetId(anime))
                        await Add(anime);
                }

                else
                    await Update(anime);
            }
        }
    }
}
