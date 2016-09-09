using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Classes.Distances;
using anime_downloader.Enums;

namespace anime_downloader.Classes.File
{
    /// <summary>
    ///     A class meant for READ-ONLY operations on accessing file paths.
    /// </summary>
    public class AnimeFileCollection
    {
        private static readonly string[] FileExtensions =
        {
            ".mkv", ".mp4", ".avi"
        };

        private readonly Settings _settings;

        public AnimeFileCollection(Settings settings)
        {
            _settings = settings;
        }

        /* Easy debug functions */

        public IEnumerable<AnimeFile> AllEpisodes => GetEpisodes(EpisodeStatus.All);

        public IEnumerable<AnimeFile> UnwatchedEpisodes => GetEpisodes(EpisodeStatus.Unwatched);

        public IEnumerable<AnimeFile> WatchedEpisodes => GetEpisodes(EpisodeStatus.Watched);

        /* */

        private IEnumerable<string> GetFilesFromStatus(EpisodeStatus episodeStatus)
        {
            IEnumerable<string> files;

            if (episodeStatus == EpisodeStatus.Unwatched)
                files = Directory.GetFiles(_settings.Paths.EpisodeDirectory, "*", SearchOption.AllDirectories);
            else if (episodeStatus == EpisodeStatus.Watched)
                files = Directory.GetFiles(_settings.Paths.WatchedDirectory, "*", SearchOption.AllDirectories);
            else // (EpisodeStatus == Episode.All)
                files = Directory.GetFiles(_settings.Paths.WatchedDirectory, "*", SearchOption.AllDirectories)
                    .Union(Directory.GetFiles(_settings.Paths.EpisodeDirectory, "*", SearchOption.AllDirectories));

            return files;
        }

        public IEnumerable<AnimeFile> GetEpisodes(EpisodeStatus episodeStatus)
        {
            try
            {
                return GetFilesFromStatus(episodeStatus)
                    .Where(filePath => FileExtensions.Any(ext => filePath.ToLower().EndsWith(ext)))
                    .Select(filePath => new AnimeFile(filePath))
                    .OrderBy(animeFile => animeFile.Name)
                    .ThenBy(animeFile => animeFile.IntEpisode);
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is ArgumentException)
            {
                return new List<AnimeFile>();
            }
        }

        public IEnumerable<AnimeFile> GetEpisodesFrom(Anime anime, EpisodeStatus episodeStatus)
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

        public IEnumerable<MultipleAnimeFiles> GetEpisodesFrom(IEnumerable<Anime> animes, EpisodeStatus episodeStatus)
        {
            return GetEpisodes(episodeStatus)
                .GroupBy(e => e.Name)
                .Select(e => new MultipleAnimeFiles {Anime = Anime.Closest.To(e.Key, animes), Episodes = e});
        }

        public AnimeFile FirstEpisodeOf(Anime anime)
        {
            return FirstEpisodeOf(GetEpisodesFrom(anime, EpisodeStatus.All));
        }

        public AnimeFile LastEpisodeOf(Anime anime)
        {
            return LastEpisodeOf(GetEpisodesFrom(anime, EpisodeStatus.All));
        }

        /* Async */

        public async Task<IEnumerable<AnimeFile>> GetEpisodesAsync(EpisodeStatus episodeStatus)
        {
            return await Task.Run(() => GetEpisodes(episodeStatus));
        }

        public async Task<IEnumerable<AnimeFile>> GetEpisodesFromAsync(Anime anime, EpisodeStatus episodeStatus)
        {
            return await Task.Run(() => GetEpisodesFrom(anime, episodeStatus));
        }

        public async Task<IEnumerable<MultipleAnimeFiles>> GetEpisodesFromAsync(IEnumerable<Anime> anime, EpisodeStatus episodeStatus)
        {
            return await Task.Run(() => GetEpisodesFrom(anime, episodeStatus));
        }

        /* Static */

        public static IEnumerable<AnimeFile> LastEpisodesOf(IEnumerable<AnimeFile> episodes)
        {
            var latest = new List<AnimeFile>();
            var reversed = episodes.OrderByDescending(animeFile => animeFile.IntEpisode);
            foreach (var anime in reversed)
                if (!latest.Any(af => af.Name.Equals(anime.Name)))
                    latest.Add(anime);
            return latest.OrderBy(af => af.Name);
        }

        public static IEnumerable<AnimeFile> FirstEpisodesOf(IEnumerable<AnimeFile> episodes)
        {
            var earliest = new List<AnimeFile>();
            foreach (var anime in episodes)
                if (!earliest.Any(af => af.Name.Equals(anime.Name)))
                    earliest.Add(anime);
            return earliest.OrderBy(af => af.Name);
        }

        public static AnimeFile FirstEpisodeOf(IEnumerable<AnimeFile> animeEpisodes)
        {
            return animeEpisodes?.OrderBy(ep => ep.IntEpisode).FirstOrDefault();
        }

        public static AnimeFile LastEpisodeOf(IEnumerable<AnimeFile> animeEpisodes)
        {
            return animeEpisodes?.OrderBy(ep => ep.IntEpisode).LastOrDefault();
        }

    }
}
