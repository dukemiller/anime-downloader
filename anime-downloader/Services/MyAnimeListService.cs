using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;
using anime_downloader.Services.Interfaces;
using HtmlAgilityPack;

namespace anime_downloader.Services
{
    public class MyAnimeListService : IMyAnimeListService
    {
        private readonly IMyAnimeListApi _api;

        private readonly IAnimeService _anime;

        public MyAnimeListService(IMyAnimeListApi api, IAnimeService anime)
        {
            _api = api;
            _anime = anime;
        }

        // 

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

        public async Task<IEnumerable<FindResult>> Find(string q) => await _api.FindAsync(q);

        public async Task Update(Anime anime)
        {
            var myAnimeListNode = anime.MyAnimeList.SeriesContinuationEpisode != null
                ? new UpdateShow(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                : new UpdateShow(anime);
            await _api.UpdateAsync(anime.MyAnimeList.Id, myAnimeListNode.ToString());
            anime.MyAnimeList.NeedsUpdating = false;
        }

        public async Task Add(Anime anime)
        {
            var myAnimeListNode = anime.MyAnimeList.SeriesContinuationEpisode != null
                ? new UpdateShow(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                : new UpdateShow(anime);
            await _api.AddAsync(anime.MyAnimeList.Id, myAnimeListNode.ToString());
            anime.MyAnimeList.NeedsUpdating = false;
        }

        public async Task<IEnumerable<Anime>> GetProfileAnime()
        {
            return (await _api.GetProfile()).Select(AnimeConverter.ToAnime);
        }

        public async Task<bool> GetId(Anime anime)
        {
            // get all results from searching the name
            var animeResults = (await _api.FindAsync(anime.Title.Replace(":", " "))).ToList();

            // if there were absolutely no results from the query
            if (!animeResults.Any())
            {
                // Continually segment words and attempt to get a result
                var name = anime.Title.Split(' ');
                var length = name.Length;
                while (!animeResults.Any() && length-- > 1)
                {
                    var newName = string.Join(" ", name.Take(length));
                    animeResults = (await _api.FindAsync(HttpUtility.UrlEncode(newName))).ToList();
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
                animeResults = (await _api.FindAsync(HttpUtility.UrlEncode(anime.Title + " (TV)"))).ToList();
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

            if (anime.MyAnimeList.TotalEpisodes <= 0)
                anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;

            if (string.IsNullOrEmpty(anime.MyAnimeList.Synopsis) || result.Synopsis.Length > anime.MyAnimeList.Synopsis?.Length)
                anime.MyAnimeList.Synopsis = result.Synopsis;

            if (string.IsNullOrEmpty(anime.MyAnimeList.Image))
                anime.MyAnimeList.Image = result.Image;

            if (string.IsNullOrEmpty(anime.MyAnimeList.Title))
                anime.MyAnimeList.Title = result.Title;

            if (string.IsNullOrEmpty(anime.MyAnimeList.English))
                anime.MyAnimeList.English = result.English;

            anime.MyAnimeList.Synonyms = result.Synonyms;

            if (anime.MyAnimeList.Aired == null)
            {
                if (DateTime.TryParse(result.StartDate, out DateTime date))
                {
                    anime.MyAnimeList.Aired = new AnimeSeason
                    {
                        Year = date.Year,
                        Season = (Season) Math.Ceiling(Convert.ToDouble(date.Month) / 3)
                    };
                }
            }

            if (DateTime.TryParse(result.EndDate, out DateTime end))
            {
                anime.MyAnimeList.Ended = new AnimeSeason
                {
                    Year = end.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(end.Month) / 3)
                };
            }

            return true;
        }

        public async Task Synchronize()
        {
            // for every anime that needs updating
            foreach (var anime in _anime.NeedsUpdates)
                if (string.IsNullOrEmpty(anime.MyAnimeList.Id))
                {
                    if (await GetId(anime))
                        await Add(anime);
                }

                else
                    await Update(anime);
        }

        public async Task<bool> Refresh(Anime anime)
        {
            if (!anime.MyAnimeList.HasId)
                return false;

            var animeResults = await Find(HttpUtility.UrlEncode(anime.MyAnimeList.Title));
            var result = animeResults.FirstOrDefault(r => r.Id.Equals(anime.MyAnimeList.Id));

            if (result == null)
                return false;

            anime.MyAnimeList.Synopsis = result.Synopsis;
            anime.MyAnimeList.Image = result.Image;
            anime.MyAnimeList.Title = result.Title;
            anime.MyAnimeList.English = result.English;
            anime.MyAnimeList.Synopsis = result.Synopsis;
            anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;

            if (DateTime.TryParse(result.StartDate, out DateTime start))
            {
                anime.MyAnimeList.Aired = new AnimeSeason
                {
                    Year = start.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(start.Month) / 3)
                };
            }

            if (DateTime.TryParse(result.EndDate, out DateTime end))
            {
                anime.MyAnimeList.Ended = new AnimeSeason
                {
                    Year = end.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(end.Month) / 3)
                };

                var now = DateTime.Now;
                anime.Airing = end.Year >= now.Year && (end.Month > now.Month ||
                                                        end.Month == now.Month && end.Day > now.Day);
            }

            return true;
        }

        public async Task<string> FindProfilePage(string text)
        {
            string result = null;

            var q = HttpUtility.UrlEncode(text);
            var document = new HtmlDocument();
            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(new Uri($"https://myanimelist.net/anime.php?q={q}"));
                document.LoadHtml(html);
            }
            var link = document.DocumentNode?
                .SelectSingleNode("//div[@class=\"js-categories-seasonal js-block-list list\"]/table/tr[2]/td[1]")?
                .Descendants("a")?
                .FirstOrDefault();

            if (link != null)
                result = link.Attributes["href"].Value;

            return result;
        }

        public async Task<FindResult> GetFindResult(Anime anime)
        {
            var animeResults = await Find(HttpUtility.UrlEncode(anime.MyAnimeList.Title));
            var result = animeResults.FirstOrDefault(r => r.Id.Equals(anime.MyAnimeList.Id));
            return result;
        }
    }
}