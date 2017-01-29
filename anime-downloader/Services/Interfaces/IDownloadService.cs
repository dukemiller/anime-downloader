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
        // 
        string ServiceName { get; }
        Task<bool> ServiceAvailable();

        // 
        Task<IEnumerable<Torrent>> FindAllTorrents(Anime anime, int episode);
        Task<IEnumerable<Torrent>> GetNextEpisode(Anime anime);
        Task<int> DownloadAll(IEnumerable<Anime> animes, Action<string> output);
        Task<int> DownloadAll(IEnumerable<Anime> animes, IEnumerable<AnimeFileRange> ranges,
            IEnumerable<AnimeFile> files, Action<string> output);
        Task<bool> AttemptDownload(Anime anime, IEnumerable<Torrent> torrents, Action<string> output);
        Task<DownloadResult> DownloadTorrent(Anime anime, Torrent torrent);
        Task<bool> DownloadEpisode(Anime anime, Torrent torrent, Action<string> output);
        bool CanDownload(Torrent torrent, Anime anime);
    }
}