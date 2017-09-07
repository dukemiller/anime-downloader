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

        public FindResult ClosestResult(Anime anime, string query, IEnumerable<FindResult> results)
        {
            var closestResults = results
                .Where(result => !result.Type.Equals("OVA")) // I'm sure i'll regret this
                .Where(result => !anime.NameStrict || result.NameCollection.Any(r => r.ToLower().Replace(" (tv)", "").Equals(query.ToLower())))
                .Select(result => new FindResultDistance(query, result))
                .OrderBy(result => result.Distance);

            return closestResults.FirstOrDefault()?.Result;
        }

        public async Task<IEnumerable<FindResult>> Find(string q) => await _api.FindAsync(q);

        public async Task Update(Anime anime)
        {
            var (successful, content) = await _api.UpdateAsync(anime);
            if (successful)
                anime.Details.NeedsUpdating = false;
        }

        public async Task Add(Anime anime) => await _api.AddAsync(anime);

        public async Task<IEnumerable<Anime>> GetProfileAnime() => (await _api.GetProfile()).Select(AnimeConverter.ToAnime);

        public async Task<bool> GetId(Anime anime)
        {
            var query = string.IsNullOrEmpty(anime.Details.PreferredSearchTitle)
                ? anime.Title
                : anime.Details.PreferredSearchTitle;

            // get all results from searching the name
            var animeResults = (await _api.FindAsync(query.Replace(":", " "))).ToList();

            // if there were absolutely no results from the query
            if (!animeResults.Any())
            {
                // Continually segment words and attempt to get a result
                var name = query.Split(' ');
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
            var result = ClosestResult(anime, query, animeResults);

            // if there was no good guess
            if (result == null)
            {
                // try slapping a (TV) infront of it because the MAL api is weird sometimes
                animeResults = (await _api.FindAsync(HttpUtility.UrlEncode(query + " (TV)"))).ToList();
                result = ClosestResult(anime, query, animeResults);

                // if still no result
                if (result == null)
                {
                    // throw an error then skip
                    Methods.Alert($"2. No partial matches found from matching names for {query}.");
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
                    result = ClosestResult(anime, query, animeResults);
                    total += result?.TotalEpisodes ?? 0;

                    // if the combination of both this season is still less than your current episode
                    // you've probably mislabeled this show for a few seasons dude, there's no way i can
                    // accurately guess which series is yours so i'll continue going through results until
                    // hopefully i can reach a point that it isnt
                    while (result != null && total < anime.Episode)
                    {
                        animeResults.Remove(result);
                        result = ClosestResult(anime, query, animeResults);
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
                    anime.Details.OverallTotal = total;
                }

            AddDataToAnime(anime, result);

            return true;
        }

        private static void AddDataToAnime(Anime anime, FindResult result)
        {

            // add all the details available
            anime.Details.Id = result.Id;

            if (anime.Details.TotalEpisodes <= 0)
                anime.Details.TotalEpisodes = result.TotalEpisodes;

            if (string.IsNullOrEmpty(anime.Details.Synopsis) || result.Synopsis.Length > anime.Details.Synopsis?.Length)
                anime.Details.Synopsis = result.Synopsis;

            if (string.IsNullOrEmpty(anime.Details.Image))
                anime.Details.Image = result.Image;

            if (string.IsNullOrEmpty(anime.Details.Title))
                anime.Details.Title = result.Title;

            if (string.IsNullOrEmpty(anime.Details.English))
                anime.Details.English = result.English;

            anime.Details.Synonyms = result.Synonyms;

            if (anime.Details.Aired == null)
            {
                if (DateTime.TryParse(result.StartDate, out DateTime date))
                {
                    anime.Details.Aired = new AnimeSeason
                    {
                        Year = date.Year,
                        Season = (Season)Math.Ceiling(Convert.ToDouble(date.Month) / 3)
                    };
                }
            }
            if (anime.Details.Ended == null)
            {
                if (DateTime.TryParse(result.EndDate, out DateTime end))
                {
                    anime.Details.Ended = new AnimeSeason
                    {
                        Year = end.Year,
                        Season = (Season) Math.Ceiling(Convert.ToDouble(end.Month) / 3)
                    };
                }
            }
        }

        public async Task Synchronize()
        {
            // for every anime that needs updating
            foreach (var anime in _anime.NeedsUpdates)
            {
                try
                {
                    // If it needs adding, add it
                    if (string.IsNullOrEmpty(anime.Details.Id))
                    {
                        if (await GetId(anime))
                            await Add(anime);
                        else
                            continue;
                    }

                    // Then edit (if just added)
                    await Update(anime);
                }

                catch (ServerProblemException spx)
                {
                    Methods.Alert(spx.StatusCode == HttpStatusCode.BadRequest
                        ? "Syncing failed, reopen the program and try again\n" +
                          "and it'll probably work. There should be a fix to this in the near future."
                        : "There's a problem with the server at the moment,\ntry syncing again in a little bit.");
                    return;
                }
            }
        }

        public async Task<bool> Refresh(Anime anime)
        {
            if (!anime.Details.HasId)
                return false;

            var animeResults = await Find(HttpUtility.UrlEncode(anime.Details.Title));
            var result = animeResults.FirstOrDefault(r => r.Id.Equals(anime.Details.Id));

            if (result == null)
                return false;

            anime.Details.Synopsis = result.Synopsis;
            anime.Details.Image = result.Image;
            anime.Details.Title = result.Title;
            anime.Details.English = result.English;
            anime.Details.Synopsis = result.Synopsis;
            anime.Details.TotalEpisodes = result.TotalEpisodes;

            if (DateTime.TryParse(result.StartDate, out DateTime start))
            {
                anime.Details.Aired = new AnimeSeason
                {
                    Year = start.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(start.Month) / 3)
                };
            }

            if (DateTime.TryParse(result.EndDate, out DateTime end))
            {
                anime.Details.Ended = new AnimeSeason
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
            var animeResults = await Find(HttpUtility.UrlEncode(anime.Details.Title));
            var result = animeResults.FirstOrDefault(r => r.Id.Equals(anime.Details.Id));
            return result;
        }
    }
}