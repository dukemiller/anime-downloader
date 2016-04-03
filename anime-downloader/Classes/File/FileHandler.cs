using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace anime_downloader.Classes.File
{
    public class FileHandler
    {
        public readonly Downloader _downloader;

        public readonly Settings _settings;

        public FileHandler(Settings settings, Downloader downloader)
        {
            _settings = settings;
            _downloader = downloader;
        }

        /// <summary>
        ///     Set all anime episode counts in the anime list to their last known values from the "watched" folder.
        /// </summary>
        /// <remarks>This is for re-indexing if you don't know which episodes you watched last.</remarks>
        public void AnimesToLastEpisode_Watched(IEnumerable<Anime> animes)
        {
            animes = animes.ToList();
            foreach (var finishedAnime in LastEpisodes(WatchedAnime()))
                Anime.ClosestTo(animes, finishedAnime.Name).Episode = finishedAnime.Episode;
        }

        /// <summary>
        ///     Set all anime episode counts in the anime list to their last known values from the episodes folder.
        /// </summary>
        /// <remarks>This is for re-indexing if you don't know which episodes you watched last.</remarks>
        public void AnimesToLastEpisode_Unwatched(IEnumerable<Anime> animes)
        {
            animes = animes.ToList();
            foreach (var finishedAnime in LastEpisodes(UnwatchedAnime()))
                Anime.ClosestTo(animes, finishedAnime.Name).Episode = finishedAnime.Episode;
        }

        public void AnimesToBeginningEpisode_All(IEnumerable<Anime> animes)
        {
            animes = animes.ToList();
            foreach (var firstAnime in FirstEpisodes(AllAnime()))
                Anime.ClosestTo(animes, firstAnime.Name).Episode = firstAnime.Episode;
        }

        /// <summary>
        ///     Find and move any duplicate files to another folder.
        /// </summary>
        /// <returns></returns>
        public async Task<int> MoveDuplicates()
        {
            var animes = UnwatchedAnime().ToList();

            // if there's another anime with the same name and episode count,
            // and it's not in the duplicate list already
            var duplicates = await Task.Run(() => animes.Where(anime => animes.Any(a => anime.Name.Equals(a.Name) &&
                                                                                        anime.Episode.Equals(a.Episode) &&
                                                                                        anime != a)).ToList());

            if (duplicates.Count > 0)
            {
                if (!Directory.Exists(_settings.DuplicatesPath))
                    Directory.CreateDirectory(_settings.DuplicatesPath);

                foreach (var duplicate in duplicates)
                    System.IO.File.Move(duplicate.FilePath, Path.Combine(_settings.DuplicatesPath, duplicate.FileName));
            }

            return duplicates.Count;
        }

        /// <summary>
        ///     Find and download any episodes in collection anime that are between the range start.episode and last.episode
        /// </summary>
        /// <param name="animes"></param>
        /// <returns></returns>
        public async Task<int> DownloadMissing(IEnumerable<Anime> animes)
        {
            var allEpisodes = await Task.Run(() => AllAnime().ToList());
            var animeFileDeltas = await Task.Run(() => FirstEpisodes(allEpisodes)
                .OrderBy(a => a.Name)
                .Zip(LastEpisodes(allEpisodes).OrderBy(a => a.Name), (a, b) => new AnimeEpisodeDelta(a, b)));
            return await _downloader.Download(animes, animeFileDeltas, allEpisodes);
        }

        /// <summary>
        ///     Get every anime not in the watched folder.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AnimeEpisode> UnwatchedAnime()
        {
            return _settings.EpisodePaths()
                .SelectMany(Directory.GetFiles)
                //.Where(file => !IsFragmentedVideo(file))
                .Select(filePath => new AnimeEpisode(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.IntEpisode);
        }

        /// <summary>
        ///     Get every anime in the watched folder.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AnimeEpisode> WatchedAnime()
        {
            return Directory.GetFiles(_settings.WatchedPath)
                //.Where(file => !IsFragmentedVideo(file))
                .Select(filePath => new AnimeEpisode(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.IntEpisode);
        }

        /// <summary>
        ///     Get every anime in every folder.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AnimeEpisode> AllAnime()
        {
            return _settings.EpisodePaths(true)
                .SelectMany(Directory.GetFiles)
                //.Where(file => !IsFragmentedVideo(file))
                .Select(filePath => new AnimeEpisode(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.IntEpisode);
        }

        /// <summary>
        ///     Get the last known episode of every anime of collection animes
        /// </summary>
        /// <param name="animes"></param>
        /// <returns></returns>
        public static IEnumerable<AnimeEpisode> LastEpisodes(IEnumerable<AnimeEpisode> animes)
        {
            var latest = new List<AnimeEpisode>();
            var reversed = animes.OrderByDescending(animeFile => animeFile.IntEpisode);
            foreach (var anime in reversed.Where(anime => !latest.Any(af => af.Name.Equals(anime.Name))))
                latest.Add(anime);
            return latest.OrderBy(af => af.Name);
        }

        /// <summary>
        ///     Get the first known episode of every anime of collection animes.
        /// </summary>
        /// <param name="animes"></param>
        /// <returns></returns>
        public static IEnumerable<AnimeEpisode> FirstEpisodes(IEnumerable<AnimeEpisode> animes)
        {
            var earliest = new List<AnimeEpisode>();
            foreach (var anime in animes.Where(anime => !earliest.Any(af => af.Name.Equals(anime.Name))))
                earliest.Add(anime);
            return earliest.OrderBy(af => af.Name);
        }

        /*
        /// <summary>
        ///     Check if the file is fragmented by some byte guesswork.
        /// </summary>
        /// <param name="fullFilepath">Full path to the file.</param>
        /// <returns></returns>
        private static bool IsFragmentedVideo(string fullFilepath) {
            byte currentByte;
            short counter = 0;

            try {
                using (var reader = new BinaryReader(System.IO.File.Open(fullFilepath, FileMode.Open))) {
                    currentByte = reader.ReadByte();
                    while (currentByte == 0) {
                        if (++counter > 10)
                            break;
                        currentByte = reader.ReadByte();
                    }
                }
            }

            catch (IOException) {
                return true;
            }

            return !(currentByte > 10);
        }
        */
    }
}