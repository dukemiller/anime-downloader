using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace anime_downloader.Classes.File
{
    public enum EpisodeType
    {
        Unwatched,
        Watched,
        All
    }

    public class FileHandler
    {
        private static readonly string[] FileExtensions =
        {
            ".mkv", ".mp4", ".avi"
        };

        private readonly Settings _settings;

        public FileHandler(Settings settings)
        {
            _settings = settings;
        }

        public async Task SetToLastAsync(List<Anime> animes, EpisodeType type)
        {
            foreach (var anime in animes)
            {
                var lastEpisode = LastEpisodeOf(await Task.Run(() => EpisodesFrom(anime, type)));
                if (lastEpisode != null)
                    anime.Episode = lastEpisode.Episode;
            }
        }

        public async Task SetToFirstAsync(List<Anime> animes, EpisodeType type)
        {
            foreach (var anime in animes)
            {
                var firstEpisode = FirstEpisodeOf(await Task.Run(() => EpisodesFrom(anime, type)));
                if (firstEpisode != null)
                    anime.Episode = firstEpisode.Episode;
            }
        }

        public void MoveEpisodeToDestination(ListBox list, string oldDirectory, string newDirectory)
        {
            var episodes = list.SelectedItems.Cast<AnimeEpisode>().ToList();

            foreach (var episode in episodes)
            {
                try
                {
                    var fullPath = episode.FilePath.Replace(oldDirectory, "").Substring(1);
                    var fragments = fullPath.Split(Path.DirectorySeparatorChar);

                    var oldPath = oldDirectory;
                    var newPath = newDirectory;

                    // If file is a directory, create in new folder, else move file
                    foreach (var fragment in fragments)
                    {
                        oldPath = Path.Combine(oldPath, fragment);
                        newPath = Path.Combine(newPath, fragment);

                        if (Directory.Exists(oldPath))
                            Directory.CreateDirectory(newPath);
                        else if (System.IO.File.Exists(oldPath))
                            System.IO.File.Move(oldPath, newPath);
                    }

                    // Delete all old folders (this would only work on the latest one because it's the only
                    // path that would be empty, TODO: fix that
                    oldPath = oldDirectory;
                    foreach (var fragment in fragments)
                    {
                        oldPath = Path.Combine(oldPath, fragment);
                        if (Directory.Exists(oldPath))
                            if (Directory.GetFiles(oldPath).Length == 0)
                            {
                                Directory.Delete(oldPath);
                                break;
                            }
                    }
                }
                catch (Exception)
                {
                    //
                }
            }
        }

        public async Task<int> MoveDuplicatesAsync()
        {
            var animes = Episodes(EpisodeType.Unwatched).ToList();

            // if there's another anime with the same name and episode count,
            // and it's not in the duplicate list already
            var duplicates = await Task.Run(() =>
                animes.Where(anime => animes.Any(a => anime.Name.Equals(a.Name) &&
                                                      anime.Episode.Equals(a.Episode) &&
                                                      anime != a)).ToList());

            if (duplicates.Count > 0)
            {
                foreach (var duplicate in duplicates)
                    System.IO.File.Move(duplicate.FilePath,
                        Path.Combine(Settings.DuplicatesDirectory, duplicate.FileName));
            }

            return duplicates.Count;
        }

        public IEnumerable<AnimeEpisode> Episodes(EpisodeType episodeType)
        {
            try
            {
                IEnumerable<string> files;

                if (episodeType == EpisodeType.Unwatched)
                    files = Directory.GetFiles(_settings.Paths.EpisodeDirectory, "*", SearchOption.AllDirectories);
                else if (episodeType == EpisodeType.Watched)
                    files = Directory.GetFiles(_settings.Paths.WatchedDirectory, "*", SearchOption.AllDirectories);
                else // (EpisodeType == Episode.All)
                    files = Directory.GetFiles(_settings.Paths.WatchedDirectory, "*", SearchOption.AllDirectories)
                        .Union(Directory.GetFiles(_settings.Paths.EpisodeDirectory, "*", SearchOption.AllDirectories));

                return files
                    .Where(filePath => FileExtensions.Any(ext => filePath.ToLower().EndsWith(ext)))
                    .Select(filePath => new AnimeEpisode(filePath))
                    .OrderBy(animeFile => animeFile.Name)
                    .ThenBy(animeFile => animeFile.IntEpisode);
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is ArgumentException)
            {
                return new List<AnimeEpisode>();
            }
        }

        public IEnumerable<AnimeEpisode> EpisodesFrom(Anime anime, EpisodeType episodeType)
        {
            return Episodes(episodeType)
                .GroupBy(e => e.Name)
                .Select(e => new { group = e, distance = Methods.LevenshteinDistance(anime.Name.RemoveWhitespace(), e.Key.RemoveWhitespace()) })
                .Where(e => e.distance <= 10)
                .OrderBy(e => e.distance)
                .FirstOrDefault()?
                .group.Select(e => e);
        }

        public IEnumerable<AnimeWithEpisodes> EpisodesFrom(IEnumerable<Anime> animes, EpisodeType episodeType)
        {
            return Episodes(episodeType)
                .GroupBy(e => e.Name)
                .Select(e => new AnimeWithEpisodes { Anime = Anime.Closest.To(e.Key, animes), Episodes = e });
        }

        public IEnumerable<AnimeEpisode> LastEpisodesOf(IEnumerable<AnimeEpisode> episodes)
        {
            var latest = new List<AnimeEpisode>();
            var reversed = episodes.OrderByDescending(animeFile => animeFile.IntEpisode);
            foreach (var anime in reversed)
                if (!latest.Any(af => af.Name.Equals(anime.Name)))
                    latest.Add(anime);
            return latest.OrderBy(af => af.Name);
        }

        public IEnumerable<AnimeEpisode> FirstEpisodesOf(IEnumerable<AnimeEpisode> episodes)
        {
            var earliest = new List<AnimeEpisode>();
            foreach (var anime in episodes)
                if (!earliest.Any(af => af.Name.Equals(anime.Name)))
                    earliest.Add(anime);
            return earliest.OrderBy(af => af.Name);
        }

        public AnimeEpisode FirstEpisodeOf(Anime anime)
        {
            return FirstEpisodeOf(EpisodesFrom(anime, EpisodeType.All));
        }

        public AnimeEpisode FirstEpisodeOf(IEnumerable<AnimeEpisode> animeEpisodes)
        {
            return animeEpisodes?.OrderBy(ep => ep.IntEpisode).FirstOrDefault();
        }

        public AnimeEpisode LastEpisodeOf(Anime anime)
        {
            return LastEpisodeOf(EpisodesFrom(anime, EpisodeType.All));
        }

        public AnimeEpisode LastEpisodeOf(IEnumerable<AnimeEpisode> animeEpisodes)
        {
            return animeEpisodes?.OrderBy(ep => ep.IntEpisode).LastOrDefault();
        }
    }
}