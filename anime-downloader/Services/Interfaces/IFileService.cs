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
        /// <summary>
        ///     Retrieve all episodes found belonging to {anime}
        /// </summary>
        IEnumerable<AnimeFile> GetEpisodes(Anime anime);

        /// <summary>
        ///     Retrieve all episodes that match the episode status description
        /// </summary>
        IEnumerable<AnimeFile> GetEpisodes(EpisodeStatus episodeStatus);

        /// <summary>
        ///     Retrieve all episodes that match the episode status description that belong to {anime}
        /// </summary>
        IEnumerable<AnimeFile> GetEpisodes(Anime anime, EpisodeStatus episodeStatus);

        /// <summary>
        ///     Retrieve all episodes that match the episode status description
        /// </summary>
        Task<IEnumerable<AnimeFile>> GetEpisodesAsync(EpisodeStatus episodeStatus);

        /// <summary>
        ///     Retrieve all episodes that match the episode status description that belong to {anime}
        /// </summary>
        Task<IEnumerable<AnimeFile>> GetEpisodesAsync(Anime anime, EpisodeStatus episodeStatus);

        /// <summary>
        ///     Retrieve the file with the lowest episode number found belonging to {anime}
        /// </summary>
        AnimeFile FirstEpisode(Anime anime);

        /// <summary>
        ///     Retrieve the file with the highest episode number found belonging to {anime}
        /// </summary>
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