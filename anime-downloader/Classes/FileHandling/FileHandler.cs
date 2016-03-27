using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace anime_downloader.Classes.FileHandling {
    public class FileHandler {
        private readonly Settings _settings;
        private readonly Downloader _downloader;
        private readonly Logger _logger;

        public FileHandler(Settings settings, Downloader downloader, Logger logger) {
            _settings = settings;
            _downloader = downloader;
            _logger = logger;
        }

        /// <summary>
        ///     Check if the file is fragmented by some byte guesswork.
        /// </summary>
        /// <param name="fullFilepath">Full path to the file.</param>
        /// <returns></returns>
        private static bool IsFragmentedVideo(string fullFilepath) {
            byte currentByte;
            short counter = 0;

            try {
                using (var reader = new BinaryReader(File.Open(fullFilepath, FileMode.Open))) {
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
        
        /// <summary>
        ///     Strip the entire path of extraneous information (subgroups, resolution, etc).
        /// </summary>
        /// <param name="fileName">A file name, not a filepath.</param>
        /// <returns></returns>
        private static string Strip(string fileName) {
            var text = fileName;

            var phrases = (from Match match in Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*")
                           select match.Groups[0].Value).ToList();

            new[] { "_", ".mp4", ".mkv", ".avi" }.ToList().ForEach(p => phrases.Add(p));

            phrases.ForEach(p => text = text.Replace(p, ""));

            // text = string.Join("-", text.Split('-').Take(text.Split('-').Length - 1).ToArray());

            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        private static int LevenshteinDistance(string s, string t) {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];
            if (n == 0)
                return m;
            if (m == 0)
                return n;
            for (var i = 0; i <= n; d[i, 0] = i++) { }
            for (var j = 0; j <= m; d[0, j] = j++) { }
            for (var i = 1; i <= n; i++) {
                for (var j = 1; j <= m; j++) {
                    var cost = t[j - 1] == s[i - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        /// <summary>
        ///     Gets the best guess to what the anime is based solely on name
        /// </summary>
        /// <param name="animes"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Anime ClosestTo(IEnumerable<Anime> animes, string name) {
            return animes.Select(a => new AnimePair {Anime = a, Distance = LevenshteinDistance(a.Name, name)})
                .OrderBy(ap => ap.Distance)
                .First()
                .Anime;
        }

        /// <summary>
        ///     Set all anime episode counts in the anime list to their last known values from the "watched" folder.
        /// </summary>
        /// <remarks>This is for re-indexing if you don't know which episodes you watched last.</remarks>
        public void IndexAnimesToWatched(IEnumerable<Anime> animes) {
            animes = animes.ToArray();
            foreach (var finishedAnime in LastEpisodes(GetWatchedAnimeFiles())) {
                var anime = ClosestTo(animes, finishedAnime.Name);
                anime.Episode = $"{finishedAnime.Episode:D2}";
            }
        }

        /// <summary>
        ///     Set all anime episode counts in the anime list to their last known values from the episodes folder.
        /// </summary>
        /// <remarks>This is for re-indexing if you don't know which episodes you watched last.</remarks>
        public void IndexAnimesToUnwatched(IEnumerable<Anime> animes) {
            animes = animes.ToArray();
            foreach (var finishedAnime in LastEpisodes(GetUnwatchedAnimeFiles())) {
                var anime = ClosestTo(animes, finishedAnime.Name);
                if (anime != null)
                    anime.Episode = $"{finishedAnime.Episode:D2}";
            }
        }

        public void ResetKnown(IEnumerable<Anime> animes) {
            animes = animes.ToArray();
            foreach (var firstAnime in FirstEpisodes(GetAllAnimeFiles())) {
                var anime = ClosestTo(animes, firstAnime.Name);
                if (anime != null)
                    anime.Episode = $"{firstAnime.Episode:D2}";
            }
        }

        public async Task<int> MoveDuplicates() {
            var animes = GetUnwatchedAnimeFiles().ToArray();

            // if there's another anime with the same name and episode count,
            // and it's not in the duplicate list already
            var duplicates = await Task.Run(() => animes.Where(anime => animes.Any(a => anime.Name.Equals(a.Name) &&
                                                                   anime.Episode.Equals(a.Episode) &&
                                                                   anime != a)).ToArray());

            if (duplicates.Length > 0) {
                if (!Directory.Exists(_settings.DuplicatesPath))
                    Directory.CreateDirectory(_settings.DuplicatesPath);

                foreach (var duplicate in duplicates)
                    File.Move(duplicate.FilePath, Path.Combine(_settings.DuplicatesPath, duplicate.FileName));
            }

            return duplicates.Length;
        }

        public async Task<int> DownloadMissing() {
            var total = 0;
            var allEpisodes = await Task.Run(() => GetAllAnimeFiles().ToArray());
            var animeFileDeltas = await Task.Run(() => FirstEpisodes(allEpisodes)
                .OrderBy(a => a.Name)
                .Zip(LastEpisodes(allEpisodes).OrderBy(a => a.Name), (a, b) => new AnimeFileDelta(a, b)));
            
            foreach (var animeFile in animeFileDeltas) {
                foreach (var episode in animeFile.Range) {

                    if (await Task.Run(() => allEpisodes.Any(a => animeFile.Name.Equals(a.Name) && a.Episode.Equals(episode))))
                        continue;

                    var anime = new Anime {Name = animeFile.Name, Episode = $"{episode:D2}"};
                    var nyaaLinks = await anime.GetLinksToNextEpisode();

                    foreach (var nyaa in nyaaLinks.Where(nyaa => _downloader.CanDownload(nyaa, anime))) {
                        await _downloader.DownloadTorrent(nyaa);
                        if (_logger?.IsEnabled ?? false)
                            await _logger.WriteLine($"Downloaded '{anime.Title}' episode {episode}.");
                        total++;
                        break;
                    }

                }
            }
            return total;
        }

        private IEnumerable<AnimeFile> GetUnwatchedAnimeFiles() {
            return _settings.EpisodePaths()
                .SelectMany(Directory.GetFiles)
                .Where(file => !IsFragmentedVideo(file))
                .Select(filePath => new AnimeFile(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.Episode);
        }

        private IEnumerable<AnimeFile> GetWatchedAnimeFiles() {
            return Directory.GetFiles(_settings.WatchedPath)
                .Where(file => !IsFragmentedVideo(file))
                .Select(filePath => new AnimeFile(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.Episode);
        }

        private IEnumerable<AnimeFile> GetAllAnimeFiles() {
            return _settings.EpisodePaths(includeWatched:true)
                .SelectMany(Directory.GetFiles)
                .Where(file => !IsFragmentedVideo(file))
                .Select(filePath => new AnimeFile(filePath))
                .OrderBy(animeFile => animeFile.Name)
                .ThenBy(animeFile => animeFile.Episode);
        }

        private static IEnumerable<AnimeFile> LastEpisodes(IEnumerable<AnimeFile> animes) {
            var latest = new List<AnimeFile>();
            var reversed = animes.OrderByDescending(animeFile => animeFile.Episode);
            foreach (var anime in reversed.Where(anime => !latest.Any(af => af.Name.Equals(anime.Name))))
                latest.Add(anime);
            return latest.OrderBy(af => af.Name);
        }

        private static IEnumerable<AnimeFile> FirstEpisodes(IEnumerable<AnimeFile> animes) {
            var earliest = new List<AnimeFile>();
            foreach (var anime in animes.Where(anime => !earliest.Any(af => af.Name.Equals(anime.Name))))
                earliest.Add(anime);
            return earliest.OrderBy(af => af.Name);
        }

        /// <summary>
        ///     All the details of an anime file as it's interpreted on the filesystem.
        /// </summary>
        private class AnimeFile {

            public AnimeFile(string filePath) {
                FilePath = filePath;
            }

            public string Name
                =>
                    string.Join("-",
                        Stripped.Split('-')
                            .Take(Stripped.Count(x => x == '-')))
                        .Trim();

            public int Episode
                =>
                    int.Parse(string.Join("",
                        Stripped.Split(new[] {" - "}, StringSplitOptions.RemoveEmptyEntries)
                            .Last()
                            .TakeWhile(char.IsNumber)));

            private string Stripped => Strip(FileName);

            public string FileName => Path.GetFileName(FilePath);

            public string FilePath { get; }
        }

        /// <summary>
        ///     The difference between an anime's first and last episode with identifying information
        /// </summary>
        private class AnimeFileDelta {

            public IEnumerable<int> Range { get; }
            public string Name { get; }

            public AnimeFileDelta(AnimeFile a, AnimeFile b) {
                Range = Enumerable.Range(a.Episode, b.Episode - a.Episode + 1);
                Name = a.Name;
            }
        }

        /// <summary>
        ///     Holds the difference compared from some string to the anime's name with identifying information
        /// </summary>
        private class AnimePair {
            public Anime Anime { get; set; }
            public int Distance { get; set; }
        }
    }
}
