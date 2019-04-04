using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Abstract;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.Classes
{
    /// <summary>
    ///     A split of responsibility from the download service to manage the heuristic behind
    ///     determining preferences.
    /// </summary>
    public static class DownloadPreference
    {
        private static DownloadServiceBase DownloadService => (DownloadServiceBase) SimpleIoc.Default.GetInstance<IDownloadService>();

        private static IAnimeRepository AnimeRepository => SimpleIoc.Default.GetInstance<IAnimeRepository>();

        private static IDetailProviderService DetailProviderService => SimpleIoc.Default.GetInstance<IDetailProviderService>();

        // 

        public static async Task<List<RemoteMedia>> DetermineSearchTitle(Anime anime, int episode)
        {
            List<RemoteMedia> englishMedia = new List<RemoteMedia>(), romanjiMedia = new List<RemoteMedia>();
            var media = new List<RemoteMedia>();

            // If this is the very start of the series, we might also need to find episode start if the show
            // is long running and indexing starts at another number (e.g. attack on titan s3 - 38 is episode 01)
            if (anime.Episode == 0)
            {
                // the countermeasure to subgroups incorrectly naming the show for whatever reason
                if (anime.Details.JustAdded)
                {
                    var english = await UncertainSearch(anime, episode, anime.Details.English);
                    var romanji = await UncertainSearch(anime, episode, anime.Details.Title);
                    if (english.Medias.Count != 0 || romanji.Medias.Count != 0)
                    {
                        media = CalculateAndSetPreference(anime, english.Medias, english.Title, romanji.Medias, romanji.Title);
                        anime.Details.JustAdded = false;
                    }
                }

                // you somehow got to episode=0 while already having added the show
                else
                {
                    englishMedia = await DownloadService.PotentialStartingEpisode(anime.Details.English)
                                   ?? await DownloadService.FindAllMedia(anime, anime.Details.English, episode);
                    romanjiMedia = await DownloadService.PotentialStartingEpisode(anime.Details.Title)
                                   ?? await DownloadService.FindAllMedia(anime, anime.Details.Title, episode);
                    if (englishMedia.Count != 0 || romanjiMedia.Count != 0)
                        media = CalculateAndSetPreference(anime, englishMedia, anime.Details.English, romanjiMedia, anime.Details.Title);
                }

                if (media.Count > 0 && !string.IsNullOrEmpty(anime.Details.PreferredSearchTitle))
                {
                    media.First().Episode.MatchSome(e => anime.Episode = e);

                    // if this is over the total, recalculate it (usually due to indexing being off like AoT example above)
                    if (anime.Episode > anime.Details.Total)
                    {
                        var previousTotal = anime.Details.Total;
                        var changed = await DetailProviderService.CheckSeriesContinuation(anime);
                        if (changed)
                        {
                            DownloadService.Messages.Enqueue($"Changed current episode for '{anime.Title}' from {episode} to {anime.Episode}.");
                            DownloadService.Messages.Enqueue($"Changed episode count for '{anime.Title}' from {previousTotal} to {anime.Details.Total}.");
                        }
                    }

                    anime.Episode--;
                }
            }

            else
            {
                if (!string.IsNullOrEmpty(anime.Details.English))
                    englishMedia = await DownloadService.FindAllMedia(anime, anime.Details.English, episode);
                if (!string.IsNullOrEmpty(anime.Details.Title))
                    romanjiMedia = await DownloadService.FindAllMedia(anime, anime.Details.Title, episode);
                if (englishMedia.Count != 0 || romanjiMedia.Count != 0)
                    media = CalculateAndSetPreference(anime, englishMedia, anime.Details.English, romanjiMedia, anime.Details.Title);
            }

            AnimeRepository.Save();

            return media;
        }

        // 

        /// <summary>
        ///     The arbitrary "health score" of a list of resources for categorizing if
        ///     trusting this selection of results is more capable of representing what
        ///     people search for
        /// </summary>
        private static int HealthScore(IReadOnlyCollection<RemoteMedia> mediaSource)
        {
            // For any "average" torrent, these would be the stats
            var count = mediaSource.Count;
            if (count == 0)
                return -1;
            var seeders = mediaSource.Select(media => media.Health).Sum() / count;
            var downloads = mediaSource.Select(m => m.Downloads.ValueOr(0)).Sum() / count;
            return (seeders + downloads) * count;
        }

        private static List<RemoteMedia> CalculateAndSetPreference(Anime anime,
            List<RemoteMedia> english, string englishTitle,
            List<RemoteMedia> romanji, string romanjiTitle)
        {
            var media = new List<RemoteMedia>();

            var (englishScore, romanjiScore) = (HealthScore(english), HealthScore(romanji));

            // Set it to romanji, but don't save any titles
            if (englishScore == -1 && romanjiScore == -1)
                media = romanjiTitle != null ? romanji : english;

            else if (englishScore > romanjiScore && englishTitle != null)
            {
                anime.Details.PreferredSearchTitle = englishTitle;
                media = english;
            }

            else if (englishScore <= romanjiScore && romanjiTitle != null)
            {
                anime.Details.PreferredSearchTitle = romanjiTitle;
                media = romanji;
            }

            return media.OrderByDescending(n => n.Name.Contains(anime.Resolution)).ThenByDescending(n => n.Health).ToList();
        }

        /// <summary>
        ///     Continually slice off the last word of the title, comparing it to the health of the
        ///     previous search until it either has less score or less than three words in the title
        /// </summary>
        private static async Task<SearchResult> UncertainSearch(Anime anime, int episode, string title)
        {
            if (string.IsNullOrEmpty(title))
                return new SearchResult(title, new List<RemoteMedia>());

            List<RemoteMedia> previousMedia = null;
            var words = title.Split();
            var count = words.Length;
            var previousScore = -1;

            do
            {
                title = string.Join(" ", words.Take(count--));
                var media = await DownloadService.PotentialStartingEpisode(title) ?? await DownloadService.FindAllMedia(anime, title, episode);
                var score = HealthScore(media);

                if (previousMedia is null)
                    previousMedia = media;

                if (score < previousScore)
                    return new SearchResult(title, previousMedia);

                previousMedia = media;
                previousScore = score;
            } while (count >= 3);

            return new SearchResult(title, previousMedia);
        }
    }

    internal readonly struct SearchResult
    {
        public SearchResult(string title, List<RemoteMedia> medias) => (Title, Medias) = (title, medias);

        public string Title { get; }

        public List<RemoteMedia> Medias { get; }
    }

}
