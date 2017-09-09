using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;

namespace anime_downloader.Repositories.Interface
{
    /// <summary>
    ///     The container for all information related to application settings.
    /// </summary>
    public interface ISettingsRepository: INotifyPropertyChanged
    {
        /// <summary>
        ///     All important user entered paths.
        /// </summary>
        PathConfiguration PathConfig { get; set; }

        /// <summary>
        ///     All true/false values used in various settings.
        /// </summary>
        FlagConfiguration FlagConfig { get; set; }

        /// <summary>
        ///     Information regarding checking updates based on version.
        /// </summary>
        VersionCheck Version { get; set; }
        
        /// <summary>
        ///     The website that will provide the source for retrieving media files from.
        /// </summary>
        DownloadProvider Provider { get; set; }

        /// <summary>
        ///     What the main anime list will be sorted by in its display.
        /// </summary>
        string SortBy { get; set; }

        /// <summary>
        ///     What the main anime list will be filtering in its display.
        /// </summary>
        string FilterBy { get; set; }

        /// <summary>
        ///     All user entered subgroups.
        /// </summary>
        List<string> Subgroups { get; set; }

        /// <summary>
        ///     Return if all directories needed for downloading are available.
        /// </summary>
        Task<bool> CrucialDirectoriesExist();

        /// <summary>
        ///     Get a command to add a new torrent to the torrent executable to download the file.
        /// </summary>
        /// <param name="torrent">The path to the .torrent file.</param>
        /// <param name="destination">The path to where the file should download to.</param>
        string TorrentDownloaderCommand(string torrent, string destination);

        /// <summary>
        ///     Save the repository to disk.
        /// </summary>
        void Save();
    }
}