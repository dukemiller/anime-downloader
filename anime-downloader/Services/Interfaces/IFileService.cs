using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Models;

namespace anime_downloader.Services.Interfaces
{
    /// <summary>
    ///     The logic behind handling file retrieval for anime episodes and
    ///     some file operations.
    /// </summary>
    public interface IFileService
    {
        IEnumerable<AnimeFile> GetEpisodes(Anime anime);
        IEnumerable<AnimeFile> GetEpisodes(EpisodeStatus episodeStatus);
        IEnumerable<AnimeFile> GetEpisodes(Anime anime, EpisodeStatus episodeStatus);
        Task<IEnumerable<AnimeFile>> GetEpisodesAsync(EpisodeStatus episodeStatus);
        Task<IEnumerable<AnimeFile>> GetEpisodesFromAsync(Anime anime, EpisodeStatus episodeStatus);

        AnimeFile FirstEpisode(Anime anime);
        AnimeFile LastEpisode(Anime anime);

        /* First or last of everything in sequence */

        /// <summary>
        ///     Retrieve the first episode of every unique series in the collection
        /// </summary>
        IEnumerable<AnimeFile> FirstEpisodes(IEnumerable<AnimeFile> files);

        /// <summary>
        ///     Retrieve the last episode of every unique series in the collection
        /// </summary>
        IEnumerable<AnimeFile> LastEpisodes(IEnumerable<AnimeFile> files);

        /* Close matches */

        /// <summary>
        ///     The closest anime file compared to {name} in {files} collection
        /// </summary>
        AnimeFile ClosestFile(IEnumerable<AnimeFile> files, string name);

        /// <summary>
        ///     The closest anime compared to {name} in {animes} collection
        /// </summary>
        Anime ClosestAnime(IEnumerable<Anime> animes, string name);

        /// <summary>
        ///     The closest anime compared to {file}'s name in {animes} collection
        /// </summary>
        Anime ClosestAnime(IEnumerable<Anime> animes, AnimeFile file);

        // 
        Task<int> MoveDuplicatesAsync();
    }
}