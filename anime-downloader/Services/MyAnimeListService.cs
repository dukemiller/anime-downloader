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
using Optional;

namespace anime_downloader.Services
{
    public class MyAnimeListService : ISyncProviderService
    {
        private readonly IMyAnimeListApi _api;

        private readonly IAnimeService _anime;

        // 

        public MyAnimeListService(IMyAnimeListApi api, IAnimeService anime)
        {
            _api = api;
            _anime = anime;
        }

        // IRequireIdentification

        public Option<int> GetId(Anime anime) => int.TryParse(anime.Details.Id, out var result) ? result.Some() : Option.None<int>();

        public void SetId(Anime anime, int id) => anime.Details.Id = id.ToString();

        // ISyncProviderService

        public async Task<Option<int>> FindId(Anime anime)
        {
            var query = string.IsNullOrEmpty(anime.Details.PreferredSearchTitle)
                ? anime.Title
                : anime.Details.PreferredSearchTitle;

            // get all results from searching the name
            var results = await _api.FindAsync(query.Replace(":", " "));

            // if we have an airing date, only accept results that are from the same season
            // this will be repeated every time
            if (anime.Details.Aired != null)
                results = results.Where(r => FilterResults(r, anime)).ToList();

            // if the japanese title hasn't been tried yet, do that first before going crazy
            if (!results.Any() && anime.Details.Title != null && query != anime.Details.Title)
            {
                results = await _api.FindAsync(anime.Details.Title.Replace(":", " "));
                if (anime.Details.Aired != null)
                    results = results.Where(r => FilterResults(r, anime)).ToList();
            }

            // if there were absolutely no results from the query
            if (!results.Any())
            {
                // Continually segment words and attempt to get a result
                var name = query.Split(' ');
                var length = name.Length;
                while (!results.Any() && length-- > 1)
                {
                    var newName = string.Join(" ", name.Take(length));
                    results = await _api.FindAsync(HttpUtility.UrlEncode(newName));
                    if (anime.Details.Aired != null)
                        results = results.Where(r => FilterResults(r, anime)).ToList();
                }

                // if after the previous operation there are still no results
                if (!results.Any())
                {
                    // throw an error then skip
                    Methods.Alert($"1. Absolutely no matching names found for {anime.Title}.");
                    return Option.None<int>();
                }
            }

            // make an estimation as to what is the closest result related to the anime
            var result = ClosestResult(anime, query, results);

            // if there was no good guess
            if (result is null)
            {
                // try slapping a (TV) infront of it because the MAL api is weird sometimes
                results = await _api.FindAsync(HttpUtility.UrlEncode(query + " (TV)"));
                if (anime.Details.Aired != null)
                    results = results.Where(r => FilterResults(r, anime)).ToList();
                result = ClosestResult(anime, query, results);

                // if still no result
                if (result is null)
                {
                    // throw an error then skip
                    Methods.Alert($"2. No partial matches found from matching names for {query}.");
                    return Option.None<int>();
                }
            }

            // check episode details if there is a given total (you can only hope)
            if (result.TotalEpisodes > 0)
                if (anime.Episode > result.TotalEpisodes)
                {
                    // track episode total
                    var total = result.TotalEpisodes;

                    // remove current series from list of possible choices
                    results.Remove(result);
                    result = ClosestResult(anime, query, results);
                    total += result?.TotalEpisodes ?? 0;

                    // if the combination of both this season is still less than your current episode
                    // you've probably mislabeled this show for a few seasons dude, there's no way i can
                    // accurately guess which series is yours so i'll continue going through results until
                    // hopefully i can reach a point that it isnt
                    while (result != null && total < anime.Episode)
                    {
                        results.Remove(result);
                        result = ClosestResult(anime, query, results);
                        total += result?.TotalEpisodes ?? 0;
                    }

                    // if we've run out of episodes, games over
                    if (result is null)
                    {
                        Methods.Alert($"3. Episode mismatch and no new series match for {anime.Title}.\n" +
                                      $"Given total: {total}, current episode: {anime.Episode}");
                        return Option.None<int>();
                    }

                    // keep track of episodes to update instead in this variable
                    anime.Details.OverallTotal = total;
                }

            AddDataToAnime(anime, result);

            return int.Parse(result.Id).Some();
        }

        public async Task Add(Anime anime)
        {
            var successful = await GetId(anime).FlatMapAsync(id => _api.AddAsync(anime, id));
            if (successful.HasValue)
                anime.Details.NeedsUpdating = false;
        }

        public async Task Update(Anime anime)
        {
            var successful = await GetId(anime).FlatMapAsync(id => _api.UpdateAsync(anime, id));
            if (successful.HasValue)
                anime.Details.NeedsUpdating = false;
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
                        var successful = await FindId(anime);
                        if (!successful.HasValue)
                            continue;
                    }

                    var profileContains = await GetId(anime).MapAsync(id => _api.ProfileContains(id));

                    // todo: figure out a cleaner way to do this
                    await profileContains.Match(
                        some: _ => _ ? Update(anime) : Add(anime), 
                        none: () => Task.Delay(0)
                    );
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

        public async Task<IEnumerable<Anime>> LoadProfile() => (await _api.GetProfile()).Select(AnimeConverter.ToAnime);

        public async Task<Option<string>> FindProfilePage(string text)
        {
            var q = HttpUtility.UrlEncode(text);
            var document = new HtmlDocument();
            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(new Uri($"https://myanimelist.net/anime.php?q={q}"));
                var node = document.LoadPage(html)?.DocumentNode
                    ?.SelectSingleNode("//div[@class=\"js-categories-seasonal js-block-list list\"]/table/tr[2]/td[1]")
                    ?.Descendants("a")?.FirstOrDefault().SomeNotNull() ?? Option.None<HtmlNode>();
                return node.Map(link => link.Attributes["href"].Value);
            }
        }

        // 

        private static bool Contains(string r, string query) => r.ToLower().Replace(" (tv)", "") == query.ToLower();

        // todo: fix this
        private static FindResult ClosestResult(Anime anime, string query, IEnumerable<FindResult> results) => results
            .Where(result => result.Type != ("OVA") && result.Type != "Movie") // I'm sure i'll regret this
            .Where(result =>
                !anime.NameStrict || Methods.Flatten<string>(result.English, result.Title, result.Synonyms.Split(';'))
                    .Any(title => Contains(title, query)))
            .Select(result => new FindResultDistance(query, result))
            .OrderBy(result => result.Distance)
            .FirstOrDefault().Result;

        private void AddDataToAnime(Anime anime, FindResult result)
        {
            // add all the details available
            SetId(anime, int.Parse(result.Id));

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

            if (anime.Details.Aired is null)
            {
                if (DateTime.TryParse(result.StartDate, out DateTime date))
                {
                    anime.Details.Aired = new AnimeSeason
                    {
                        Year = date.Year,
                        Season = (Season) Math.Ceiling(Convert.ToDouble(date.Month) / 3)
                    };
                }
            }

            if (anime.Details.Ended is null)
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

        private static bool FilterResults(FindResult result, Anime anime)
        {
            if (DateTime.TryParse(result.StartDate, out var date))
            {
                var (year, month) = (anime.Details.Aired.Year, anime.Details.Aired.Season.ToFirstMonthAired());
                var comparedDate = new DateTime(year, month, 1);
                return date >= comparedDate;
            }

            return false;
        }
    }
}