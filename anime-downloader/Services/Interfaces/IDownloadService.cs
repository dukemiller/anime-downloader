using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;

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
        ///     The url for the front page of the service.
        /// </summary>
        string ServiceUrl { get; }

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
        ///     Download specific given episodes numbers of every series in {animes} outputting result in {output}
        /// </summary>
        Task<int> DownloadSpecificEpisodes(Dictionary<Anime, List<int>> animes, Action<string> output);

        /// <summary>
        ///     Retrieve a (list, health sum) of the potential starting episodes for a given series.
        /// </summary>
        Task<List<RemoteMedia>> PotentialStartingEpisode(string name);

        /// <summary>
        ///     Gathers every episode (unfiltered by settings) for {anime}
        /// </summary>
        Task<List<RemoteMedia>> FindAllMedia(Anime anime, string name, int episode);

        /// <summary>
        ///     Gathers every episode (unfiltered by settings) for {anime}, automatically determining the appropriate name.
        /// </summary>
        Task<List<RemoteMedia>> FindAllMedia(Anime anime, int episode);
        
        /// <summary>
        ///     Attempt to start a single piece of media from the list of media.
        /// </summary>
        Task<bool> AttemptDownload(Anime anime, int episode, IEnumerable<RemoteMedia> medias, Action<string> output);

        /// <summary>
        ///     Downloads the media for the given anime episode.
        /// </summary>
        Task<(bool successful, string command)> DownloadMedia(Anime anime, RemoteMedia media);

        /// <summary>
        ///     Initiate the media.
        /// </summary>
        void StartMedia(RemoteMedia media, string command);

        /// <summary>
        ///     Attempts to download the episode and returns the result of doing so.
        /// </summary>
        Task<bool> DownloadEpisode(Anime anime, int episode, RemoteMedia media, Action<string> output);

        /// <summary>
        ///     Check if able to download the media based on setting rules and anime matching checks.
        /// </summary>
        bool CanDownload(RemoteMedia media, Anime anime);
    }
}