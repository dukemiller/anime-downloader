using System.Collections.Generic;
using System.ComponentModel;
using anime_downloader.Models;

namespace anime_downloader.Repositories.Interface
{
    public interface IAnimeRepository: INotifyPropertyChanged
    {
        List<Anime> Animes { get; set; }
        void Save();
    }
}