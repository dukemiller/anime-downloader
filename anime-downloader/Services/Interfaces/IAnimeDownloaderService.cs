using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;

namespace anime_downloader.Services.Interfaces
{
    /// <summary>
    ///     The logic for a downloading host provider for anime episodes, usually in 
    ///     the form of a torrent host.
    /// </summary>
    public interface IAnimeDownloaderService
    {
        // 
        string ServiceName { get; }
        Task<bool> ServiceAvailable();

        // 
        Task<IEnumerable<Torrent>> GetTorrentsAsync(Anime anime, int episode);
        Task<int> DownloadAsync(IEnumerable<Anime> animes, Action<string> output);
        Task<int> DownloadAsync(IEnumerable<Anime> animes, IEnumerable<AnimeFileRange> ranges,
            IEnumerable<AnimeFile> files, Action<string> output);
        Task<bool> DownloadFileAsync(Torrent torrent, Anime anime);
        Task<bool> DownloadEpisodeAsync(IEnumerable<Torrent> torrents, Anime anime, Action<string> output);
        Task<bool> DownloadTorrentAsync(Torrent torrent, Anime anime, Action<string> output);
        bool CanDownload(Torrent torrent, Anime anime);
    }
}