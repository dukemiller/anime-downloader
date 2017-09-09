using System.Collections.Generic;
using System.ComponentModel;
using anime_downloader.Models;

namespace anime_downloader.Repositories.Interface
{
    /// <summary>
    ///     The container for all of the anime data.
    /// </summary>
    public interface IAnimeRepository: INotifyPropertyChanged
    {
        /// <summary>
        ///     All held anime information.
        /// </summary>
        List<Anime> Animes { get; set; }

        /// <summary>
        ///     Save the repository to disk.
        /// </summary>
        void Save();
    }
}