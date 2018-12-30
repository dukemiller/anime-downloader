using System.ComponentModel;
using anime_downloader.Models.Configurations;

namespace anime_downloader.Repositories.Interface
{
    /// <summary>
    ///     The container for all information related to user credentials.
    /// </summary>
    public interface ICredentialsRepository: INotifyPropertyChanged
    {
        /// <summary>
        ///     All information specific to myanimelist.
        /// </summary>
        MyAnimeListConfiguration MyAnimeListConfig { get; set; }
        
        /// <summary>
        ///     Save the repository to disk.
        /// </summary>
        void Save();
    }
}