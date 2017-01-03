using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Xml.Serialization;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    // http://myanimelist.net/modules.php?go=api
    public class MyAnimeListService : IMyAnimeListService
    {
        private const string ApiSearch = "https://myanimelist.net/api/anime/search.xml?q={0}";

        private const string ApiAdd = "https://myanimelist.net/api/animelist/add/{0}.xml";

        private const string ApiUpdate = "https://myanimelist.net/api/animelist/update/{0}.xml";

        private const string ApiDelete = "https://myanimelist.net/api/animelist/delete/{0}.xml";

        private const string ApiVerify = "https://myanimelist.net/api/account/verify_credentials.xml";

        private const string ApiProfile = "https://myanimelist.net/malappinfo.php?u={0}&status=all&type=anime";

        private static readonly XmlSerializer ResultDeserializer = new XmlSerializer(typeof(FindResultRoot));

        private static readonly XmlSerializer ProfileDeserializer = new XmlSerializer(typeof(ProfileResult));

        // 

        public MyAnimeListService(ISettingsService settings, IAnimeService anime)
        {
            Settings = settings;
            Anime = anime;
        }

        // 

        private ISettingsService Settings { get; }

        private IAnimeService Anime { get; }

        // Interface methods

        public NetworkCredential GetCredentials() => new NetworkCredential(Settings.MyAnimeListConfig.Username, Settings.MyAnimeListConfig.Password);

        public async Task<List<FindResult>> Find(string q) => await FindAsync(q);

        public async Task<bool> VerifyCredentialsAsync()
        {
            const string url = ApiVerify;
            var handler = new HttpClientHandler {Credentials = GetCredentials()};
            var client = new HttpClient(handler);
            var response = await client.GetAsync(url);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public FindResult ClosestResult(Anime anime, IEnumerable<FindResult> results)
        {
            var closestResults = results
                .Where(result => !result.Type.Equals("OVA")) // I'm sure i'll regret this
                .Where(result =>
                {
                    if (!anime.NameStrict)
                        return true;
                    return result.NameCollection.Any(r => r.ToLower().Replace(" (tv)", "").Equals(anime.Name.ToLower()));
                })
                //.Where(findResult =>
                //{
                //    if (findResult.TotalEpisodes != 0)
                //        return findResult.TotalEpisodes > 2;
                //    return true;
                //})
                .Select(result => new FindResultDistance(anime.Name, result))
                .OrderBy(resultDistance => resultDistance.Distance);

            var closest = closestResults.FirstOrDefault();

            // if any values have the same exact distance
            if (closestResults.Any(c => closest?.Distance == c.Distance))
                closest = closestResults.Where(c => c.Distance == closest?.Distance)
                    .OrderByDescending(r => DateTime.Parse(r.FindResult.StartDate))
                    .FirstOrDefault();

            return closest?.FindResult;
        }

        public async Task Update(Anime anime)
        {
            var myAnimeListNode = anime.MyAnimeList.SeriesContinuationEpisode != null
                ? new UpdateShow(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                : new UpdateShow(anime);
            await UpdateAsync(anime.MyAnimeList.Id, myAnimeListNode.ToString());
            anime.MyAnimeList.NeedsUpdating = false;
        }

        public async Task Add(Anime anime)
        {
            var myAnimeListNode = anime.MyAnimeList.SeriesContinuationEpisode != null
                ? new UpdateShow(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                : new UpdateShow(anime);
            await AddAsync(anime.MyAnimeList.Id, myAnimeListNode.ToString());
            anime.MyAnimeList.NeedsUpdating = false;
        }

        public async Task<IEnumerable<Anime>> GetProfileAnime()
        {
            var animes = await GetProfile();

            return animes
                .Select(m =>
                {
                    bool airing;
                    switch (m.SeriesStatus)
                    {
                        case "1":
                            airing = true;
                            break;

                        default:
                            airing = false;
                            break;
                    }

                    Status status;
                    switch (m.My_status)
                    {
                        case "1":
                            status = Status.Watching;
                            break;
                        case "3":
                            status = Status.OnHold;
                            break;
                        case "4":
                            status = Status.Dropped;
                            break;
                        case "6":
                            status = Status.Considering;
                            break;
                        case "2":
                        default:
                            status = Status.Finished;
                            break;
                    }
                    
                    int episodes;
                    var episodesResult = int.TryParse(m.MyWatchedEpisodes, out episodes);

                    int seriesEpisodes;
                    var seriesResult = int.TryParse(m.SeriesEpisodes, out seriesEpisodes);

                    var anime = new Anime
                    {
                        Name = m.SeriesTitle,
                        Episode = episodesResult ? episodes : 0,
                        Rating = m.My_score,
                        Notes = m.My_tags,
                        Status = status,
                        Airing = airing,
                        Resolution = "720",
                        MyAnimeList = new MyAnimeListDetails
                        {
                            Id = m.SeriesAnimedbId,
                            Synonyms = m.SeriesSynonyms,
                            Image = m.SeriesImage,
                            Title = m.SeriesTitle,
                            TotalEpisodes = seriesResult ? seriesEpisodes : 0,
                            NeedsUpdating = false,
                            English = "",
                            Synopsis = ""
                        }
                    };

                    // Date stuff
                    DateTime startAiring;
                    if (DateTime.TryParse(m.SeriesStart, out startAiring))
                    {
                        anime.MyAnimeList.Aired = new AnimeSeason
                        {
                            Year = startAiring.Year,
                            Season = (Season) Math.Ceiling(Convert.ToDouble(startAiring.Month) / 3)
                        };
                    }

                    DateTime endAiring;
                    if (DateTime.TryParse(m.SeriesEnd, out endAiring))
                    {
                        anime.MyAnimeList.Ended = new AnimeSeason
                        {
                            Year = endAiring.Year,
                            Season = (Season) Math.Ceiling(Convert.ToDouble(endAiring.Month) / 3)
                        };
                    }
                    
                    if (anime.Rating.Equals("0"))
                        anime.Rating = "";

                    return anime;
                });
        }

        public async Task<bool> GetId(Anime anime)
        {
            // get all results from searching the name
            var animeResults = await FindAsync(anime.Title.Replace(":", ""));

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
            var result = ClosestResult(anime, animeResults);

            // if there was no good guess
            if (result == null)
            {
                // try slapping a (TV) infront of it because the MAL api is weird sometimes
                animeResults = await FindAsync(HttpUtility.UrlEncode(anime.Title + " (TV)"));
                result = ClosestResult(anime, animeResults);

                // if still no result
                if (result == null)
                {
                    // throw an error then skip
                    Methods.Alert($"2. No partial matches found from matching names for {anime.Title}.");
                    return false;
                }
            }

            // check episode details if there is a given total (you can only hope)
            if (result.TotalEpisodes > 0)
                if (anime.Episode > result.TotalEpisodes)
                {
                    // track episode total
                    var total = result.TotalEpisodes;

                    // remove current series from list of possible choices
                    animeResults.Remove(result);
                    result = ClosestResult(anime, animeResults);
                    total += result?.TotalEpisodes ?? 0;

                    // if the combination of both this season is still less than your current episode
                    // you've probably mislabeled this show for a few seasons dude, there's no way i can
                    // accurately guess which series is yours so i'll continue going through results until
                    // hopefully i can reach a point that it isnt
                    while (result != null && total < anime.Episode)
                    {
                        animeResults.Remove(result);
                        result = ClosestResult(anime, animeResults);
                        total += result?.TotalEpisodes ?? 0;
                    }

                    // if we've run out of episodes, games over
                    if (result == null)
                    {
                        Methods.Alert($"3. Episode mismatch and no new series match for {anime.Title}.\n" +
                                      $"Given total: {total}, current episode: {anime.Episode}");
                        return false;
                    }

                    // keep track of episodes to update instead in this variable
                    anime.MyAnimeList.SeriesContinuationEpisode = (anime.Episode - total).ToString();
                    anime.MyAnimeList.OverallTotal = total;
                }

            // add all the details available
            anime.MyAnimeList.Id = result.Id;
            anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;
            anime.MyAnimeList.Synopsis = result.Synopsis;
            anime.MyAnimeList.Image = result.Image;
            anime.MyAnimeList.Title = result.Title;
            anime.MyAnimeList.English = result.English;
            anime.MyAnimeList.Synonyms = result.Synonyms;
            DateTime date;
            if (DateTime.TryParse(result.StartDate, out date))
            {
                anime.MyAnimeList.Aired = new AnimeSeason
                {
                    Year = date.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(date.Month) / 3)
                };
            }

            return true;
        }

        public async Task Synchronize()
        {
            // for every anime that needs updating
            foreach (var anime in Anime.NeedsUpdates)
                if (string.IsNullOrEmpty(anime.MyAnimeList.Id))
                {
                    if (await GetId(anime))
                        await Add(anime);
                }

                else
                {
                    await Update(anime);
                }
        }

        // API requests

        private async Task<IEnumerable<ProfileAnimeResult>> GetProfile()
        {
            var url = string.Format(ApiProfile, Settings.MyAnimeListConfig.Username);
            var request = await GetAsync(url);
            var data = await request.ReadAsStreamAsync();
            if (data == null || data.Length <= 0)
                return new List<ProfileAnimeResult>();
            using (var response = new StreamReader(data))
            {
                var result = (ProfileResult)ProfileDeserializer.Deserialize(response);
                return result.Anime.Where(anime =>
                {
                    DateTime date;
                    var withinLastThreeYears = DateTime.TryParse(anime?.SeriesStart, out date) && Math.Abs(DateTime.Now.Year - date.Year) <= 3;
                    var isShortOrSeries = anime?.SeriesType?.Equals("1") == true || anime?.SeriesType?.Equals("2") == true;
                    var definitelyNotAnOva = int.Parse(anime?.SeriesEpisodes ?? "0") > 4;
                    return withinLastThreeYears && isShortOrSeries && definitelyNotAnOva;
                });
            }

        }

        private async Task<HttpContent> GetAsync(string url)
        {
            var handler = new HttpClientHandler {Credentials = GetCredentials()};
            var client = new HttpClient(handler);
            var response = (await client.GetAsync(url)).Content;
            return response;
        }

        private async Task<HttpContent> PostAsync(string url, string data)
        {
            var handler = new HttpClientHandler {Credentials = GetCredentials()};
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
            q = HttpUtility.UrlPathEncode(q).Replace("%20", "%25");
            var url = string.Format(ApiSearch, q);
            var request = await GetAsync(url);
            var data = await request.ReadAsStreamAsync();
            if (data == null || data.Length <= 0)
                return new List<FindResult>();
            using (var response = new StreamReader(data))
            {
                var result = (FindResultRoot) ResultDeserializer.Deserialize(response);
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
        
    }
}