using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace anime_downloader.Classes.File
{
    public class FileHandler
    {
        private readonly Settings _settings;

        public FileHandler(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        ///     Set all anime episode counts in the anime list to their last known values from the "watched" folder.
        /// </summary>
        /// <remarks>This is for re-indexing if you don't know which episodes you watched last.</remarks>
        public void AnimeEpisodesToLastEpisode_Watched(IEnumerable<Anime> animes)
        {
            animes = animes.ToList();
            foreach (var finishedAnime in LastEpisodesOf(WatchedAnimeEpisodes()))
                Anime.ClosestTo(animes, finishedAnime.Name).Episode = finishedAnime.Episode;
        }

        /// <summary>
        ///     Set all anime episode counts in the anime list to their last known values from the episodes folder.
        /// </summary>
        /// <remarks>This is for re-indexing if you don't know which episodes you watched last.</remarks>
        public void AnimeEpisodesToLastEpisode_Unwatched(IEnumerable<Anime> animes)
        {
            animes = animes.ToList();
            foreach (var finishedAnime in LastEpisodesOf(UnwatchedAnimeEpisodes()))
                Anime.ClosestTo(animes, finishedAnime.Name).Episode = finishedAnime.Episode;
        }

        public void AnimeEpisodesToBeginningEpisode_All(IEnumerable<Anime> animes)
        {
            animes = animes.ToList();
            foreach (var firstAnime in FirstEpisodesOf(AllAnimeEpisodes()))
                Anime.ClosestTo(animes, firstAnime.Name).Episode = firstAnime.Episode;
        }

        /// <summary>
        ///     Find and move any duplicate files to another folder.
        /// </summary>
        /// <returns></returns>
        public async Task<int> MoveDuplicatesAsync()
        {
            var animes = UnwatchedAnimeEpisodes().ToList();

            // if there's another anime with the same name and episode count,
            // and it's not in the duplicate list already
            var duplicates = await Task.Run(() => animes.Where(anime => animes.Any(a => anime.Name.Equals(a.Name) &&
                                                                                        anime.Episode.Equals(a.Episode) &&
                                                                                        anime != a)).ToList());

            if (duplicates.Count > 0)
            {
                if (!Directory.Exists(_settings.DuplicatesDirectory))
                    Directory.CreateDirectory(_settings.DuplicatesDirectory);

                foreach (var duplicate in duplicates)
                    System.IO.File.Move(duplicate.FilePath,
                        Path.Combine(_settings.DuplicatesDirectory, duplicate.FileName));
            }

            return duplicates.Count;
        }

        /// <summary>
        ///     Get every anime not in the watched folder.
        /// </summary>
        public IEnumerable<AnimeEpisode> UnwatchedAnimeEpisodes()
        {
            return _settings.EpisodeDirectories()
                .SelectMany(Directory.GetFiles)
                .Select(filePath => new AnimeEpisode(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.IntEpisode);
        }

        /// <summary>
        ///     Get every anime in the watched folder.
        /// </summary>
        public IEnumerable<AnimeEpisode> WatchedAnimeEpisodes()
        {
            return Directory.GetFiles(_settings.WatchedDirectory)
                .Select(filePath => new AnimeEpisode(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.IntEpisode);
        }

        /// <summary>
        ///     Get every anime in every folder.
        /// </summary>
        public IEnumerable<AnimeEpisode> AllAnimeEpisodes()
        {
            return _settings.EpisodeDirectories(true)
                .SelectMany(Directory.GetFiles)
                .Select(filePath => new AnimeEpisode(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.IntEpisode);
        }

        /// <summary>
        ///     Get the last known episode of every anime in an AnimeEpisode collection.
        /// </summary>
        public IEnumerable<AnimeEpisode> LastEpisodesOf(IEnumerable<AnimeEpisode> episodes)
        {
            var latest = new List<AnimeEpisode>();
            var reversed = episodes.OrderByDescending(animeFile => animeFile.IntEpisode);
            foreach (var anime in reversed.Where(anime => !latest.Any(af => af.Name.Equals(anime.Name))))
                latest.Add(anime);
            return latest.OrderBy(af => af.Name);
        }

        /// <summary>
        ///     Get the first known episode of every anime in an AnimeEpisode collection.
        /// </summary>
        public IEnumerable<AnimeEpisode> FirstEpisodesOf(IEnumerable<AnimeEpisode> episodes)
        {
            var earliest = new List<AnimeEpisode>();
            foreach (var anime in episodes.Where(anime => !earliest.Any(af => af.Name.Equals(anime.Name))))
                earliest.Add(anime);
            return earliest.OrderBy(af => af.Name);
        }

        public AnimeEpisode LastEpisodeOf(Anime anime)
        {
            var allAnime = AllAnimeEpisodes().ToList();
            var episodeFileName = Anime.ClosestTo(allAnime.Select(a => a.Name).Distinct(), anime.Name);
            return allAnime.FirstOrDefault(ae => ae.Name.Equals(episodeFileName) &&
                                                 ae.Episode.Equals(anime.Episode));
        }

        public AnimeEpisode GetAnimeEpisode(Anime anime, int episode)
        {
            var episodeFileName = Anime.ClosestTo(AllAnimeEpisodes().Select(a => a.Name).Distinct(), anime.Name);
            return AllAnimeEpisodes().First(ae => ae.Name.Equals(episodeFileName) && ae.Episode.Equals($"{episode:D1}"));
        }
    }
}