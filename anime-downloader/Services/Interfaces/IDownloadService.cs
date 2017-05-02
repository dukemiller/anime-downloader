using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Models;

namespace anime_downloader.Services.Interfaces
{
    /// <summary>
    ///     The logic for a downloading host provider for anime episodes, usually in
    ///     the form of a torrent host.
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        ///     The name of the host provider service.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        ///     Determine if the service is online.
        /// </summary>
        Task<bool> ServiceAvailable();

        // 

        /// <summary>
        ///     Download the latest episode of every series in {animes} outputting the result in {output}
        /// </summary>
        Task<int> DownloadAll(IEnumerable<Anime> animes, Action<string> output);

        /// <summary>
        ///     Download the latest episode of every series in {animes} between the file ranges in {ranges}
        /// </summary>
        Task<int> DownloadAll(IEnumerable<Anime> animes, IEnumerable<AnimeFileRange> ranges, IEnumerable<AnimeFile> files, Action<string> output);

        /// <summary>
        ///     Gathers every episode (unfiltered by settings) for {anime}
        /// </summary>
        Task<IEnumerable<Torrent>> FindAllTorrents(Anime anime, int episode);

        /// <summary>
        ///     Get the a list of torrents for the next episode in succession for the show.
        /// </summary>
        Task<IEnumerable<Torrent>> GetNextEpisode(Anime anime);

        /// <summary>
        ///     Attempt to download a single torrent from the list of torrents.
        /// </summary>
        Task<bool> AttemptDownload(Anime anime, IEnumerable<Torrent> torrents, Action<string> output);

        /// <summary>
        ///     Downloads the torrent for the given anime episode.
        /// </summary>
        Task<DownloadResult> DownloadTorrent(Anime anime, Torrent torrent);

        /// <summary>
        ///     Attempts to download the episode and returns the result of doing so.
        /// </summary>
        Task<bool> DownloadEpisode(Anime anime, Torrent torrent, Action<string> output);

        /// <summary>
        ///     Check if able to download the torrent based on setting rules and anime matching checks.
        /// </summary>
        bool CanDownload(Torrent torrent, Anime anime);
    }
}