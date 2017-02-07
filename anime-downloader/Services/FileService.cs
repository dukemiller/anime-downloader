using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class FileService : IFileService
    {
        private static readonly string[] FileExtensions =
        {
            ".mkv", ".mp4", ".avi"
        };

        public FileService(ISettingsService settings)
        {
            _settings = settings;
        }

        private readonly ISettingsService _settings;

        /* Easy debug functions */

        public IEnumerable<AnimeFile> AllEpisodes => GetEpisodes(EpisodeStatus.All);

        public IEnumerable<AnimeFile> UnwatchedEpisodes => GetEpisodes(EpisodeStatus.Unwatched);

        public IEnumerable<AnimeFile> WatchedEpisodes => GetEpisodes(EpisodeStatus.Watched);

        public IEnumerable<AnimeFile> GetEpisodes(Anime anime)
        {
            var episodes = GetEpisodes(EpisodeStatus.All).ToList();

            var name = episodes
                .Select(e => e.Name)
                .Distinct()
                .Select(e => new {Name = e, Distance = Methods.LevenshteinDistance(anime.Name, e)})
                .OrderBy(e => e.Distance)
                .First()
                .Name;

            return episodes.Where(e => e.Name.Equals(name));
        }

        public IEnumerable<AnimeFile> GetEpisodes(EpisodeStatus episodeStatus)
        {
            try
            {
                return GetFilesFromStatus(episodeStatus)
                    .Where(filePath => FileExtensions.Any(ext => filePath.ToLower().EndsWith(ext)))
                    .Select(filePath => new AnimeFile(filePath))
                    .OrderBy(animeFile => animeFile.Name)
                    .ThenBy(animeFile => animeFile.Episode);
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is ArgumentException)
            {
                return new List<AnimeFile>();
            }
        }

        public IEnumerable<AnimeFile> GetEpisodes(Anime anime, EpisodeStatus episodeStatus)
        {
            var collection = GetEpisodes(episodeStatus)
                .GroupBy(e => e.Name)
                .Select(e => new GroupFileDistance(e, anime))
                .Where(e => e.Distance <= 25)
                .OrderBy(e => e.Distance)
                .FirstOrDefault()?.Group
                .Select(e => e);
            return collection ?? new List<AnimeFile>();
        }

        public AnimeFile FirstEpisode(Anime anime)
        {
            return FirstEpisode(GetEpisodes(anime, EpisodeStatus.All));
        }

        public AnimeFile LastEpisode(Anime anime)
        {
            return LastEpisode(GetEpisodes(anime, EpisodeStatus.All));
        }

        /* Async */

        public async Task<IEnumerable<AnimeFile>> GetEpisodesAsync(EpisodeStatus episodeStatus)
        {
            return await Task.Run(() => GetEpisodes(episodeStatus));
        }

        public async Task<IEnumerable<AnimeFile>> GetEpisodesFromAsync(Anime anime, EpisodeStatus episodeStatus)
        {
            return await Task.Run(() => GetEpisodes(anime, episodeStatus));
        }

        /* Static */

        public IEnumerable<AnimeFile> LastEpisodes(IEnumerable<AnimeFile> files)
        {
            var latest = new List<AnimeFile>();
            var reversed = files.OrderByDescending(animeFile => animeFile.Episode);
            foreach (var anime in reversed)
                if (!latest.Any(af => af.Name.Equals(anime.Name)))
                    latest.Add(anime);
            return latest.OrderBy(af => af.Name);
        }

        public IEnumerable<AnimeFile> FirstEpisodes(IEnumerable<AnimeFile> files)
        {
            var earliest = new List<AnimeFile>();
            foreach (var anime in files)
                if (!earliest.Any(af => af.Name.Equals(anime.Name)))
                    earliest.Add(anime);
            return earliest.OrderBy(af => af.Name);
        }

        /* Closest */

        /// A set of retrieval methods for finding anime in the collection without needing to strum up
        /// linq methods, getting the best guess to what the anime is based solely on the given input string
        public AnimeFile ClosestFile(IEnumerable<AnimeFile> files, string name)
        {
            return files
                .Select(a => new {Anime = a, Distance = Methods.LevenshteinDistance(a.Name, name)})
                .Where(ap => ap.Distance <= 10)
                .OrderBy(ap => ap.Distance)
                .FirstOrDefault()?.Anime;
        }

        public Anime ClosestAnime(IEnumerable<Anime> animes, AnimeFile file)
        {
            return animes
                .Select(anime => new StringDistance<Anime>(anime, file.Name, anime.Name))
                .Where(pair => pair.Distance <= 10)
                .OrderBy(pair => pair.Distance)
                .FirstOrDefault()?.Item;
        }

        public Anime ClosestAnime(IEnumerable<Anime> animes, string name)
        {
            return animes
                .Select(anime => new StringDistance<Anime>(anime, name, anime.Name))
                .Where(pair => pair.Distance <= 10)
                .OrderBy(pair => pair.Distance)
                .FirstOrDefault()?.Item;
        }

        [NeedsUpdating]
        public async Task<int> MoveDuplicatesAsync()
        {
            var animeEpisodes = (await GetEpisodesAsync(EpisodeStatus.Unwatched)).ToList();

            // if there's another anime with the same name and episode count,
            // and it's not in the duplicate list already
            var duplicates = animeEpisodes.Where(episode =>
                animeEpisodes.Any(otherEpisode => episode != otherEpisode &&
                                                  episode.Name.Equals(otherEpisode.Name) &&
                                                  episode.Episode == otherEpisode.Episode
                )).ToList();

            if (duplicates.Any())
                foreach (var duplicate in duplicates)
                    File.Move(duplicate.Path,
                        Path.Combine(_settings.PathConfig.DuplicatesDirectory, duplicate.FileName));

            return duplicates.Count;
        }

        /* */

        private IEnumerable<string> GetFilesFromStatus(EpisodeStatus episodeStatus)
        {
            IEnumerable<string> files;

            if (episodeStatus == EpisodeStatus.Unwatched)
                files = Directory.GetFiles(_settings.PathConfig.Unwatched, "*", SearchOption.AllDirectories);
            else if (episodeStatus == EpisodeStatus.Watched)
                files = Directory.GetFiles(_settings.PathConfig.Watched, "*", SearchOption.AllDirectories);
            else // (EpisodeStatus == Episode.All)
                files = Directory.GetFiles(_settings.PathConfig.Watched, "*", SearchOption.AllDirectories)
                    .Union(Directory.GetFiles(_settings.PathConfig.Unwatched, "*", SearchOption.AllDirectories));

            return files;
        }

        public static AnimeFile FirstEpisode(IEnumerable<AnimeFile> episodes)
        {
            return episodes?.OrderBy(ep => ep.Episode).FirstOrDefault();
        }

        public static AnimeFile LastEpisode(IEnumerable<AnimeFile> episodes)
        {
            return episodes?.OrderBy(ep => ep.Episode).LastOrDefault();
        }
    }
}