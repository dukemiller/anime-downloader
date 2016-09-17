using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace anime_downloader.Classes.Web.MyAnimeList
{
    public static class Synchronizer
    {
        public static async Task<bool> GetId(Anime anime, ICredentials credentials)
        {
            // get all results from searching the name
            var animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(anime.Title));

            // if there were absolutely no results from the query
            if (!animeResults.Any())
            {
                // Continually segment words and attempt to get a result
                var name = anime.Title.Split(' ');
                var length = name.Length;
                while (!animeResults.Any() && length-- > 1)
                {
                    animeResults =
                        await
                            Api.FindAsync(credentials,
                                HttpUtility.UrlEncode(string.Join(" ", name.Take(length))));
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
                animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(anime.Title + " (TV)"));
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
                    // you've probably mislabled this show for a few seasons dude, there's no way i can
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

        // The type signature of api call methods
        private delegate Task<HttpContent> ApiFunction(ICredentials credentials, string id, string data);

        private static async Task CallMalApi(Anime anime, ICredentials credentials, ApiFunction apiFunction)
        {
            // TODO fix this for any season over S2
            var myAnimeListNode = !anime.MyAnimeList.SeriesContinuationEpisode.IsBlank()
                ? new UpdateShow(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                : new UpdateShow(anime);

            // update the data
            await apiFunction(credentials, anime.MyAnimeList.Id, myAnimeListNode.ToString());

            // reset flag to update
            anime.MyAnimeList.NeedsUpdating = false;
        }

        private static async Task UpdateMal(Anime anime, ICredentials credentials) => await CallMalApi(anime, credentials, Api.UpdateAsync);

        private static async Task AddMal(Anime anime, ICredentials credentials) => await CallMalApi(anime, credentials, Api.AddAsync);

        // 

        public static async Task FullSynchronize()
        {
            // Get credentials
            var credentials = Api.GetCredentials(MainWindow.Window.Settings);

            // for every anime that needs updating
            foreach (var anime in MainWindow.Window.AnimeCollection.NeedsUpdates)
            {
                // if no id is found
                if (anime.MyAnimeList.Id.IsBlank())
                {
                    if (await GetId(anime, credentials))
                        await AddMal(anime, credentials);
                }

                else
                    await UpdateMal(anime, credentials);
            }
        }

    }
}
