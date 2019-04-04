using System;
using System.Threading.Tasks;
using anime_downloader.Enums;

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
        string Name { get; }

        /// <summary>
        ///     The url for the front page of the service.
        /// </summary>
        string Url { get; }

        /// <summary>
        ///     Determine if the service is online.
        /// </summary>
        Task<bool> Available();

        /// <summary>
        ///     Attempt the download option, returning the number of started torrent files.
        /// </summary>
        Task<int> Download(DownloadOption option, Action<string> output);
    }
}