using System.Collections.Generic;
using anime_downloader.Models;

namespace anime_downloader.Repositories.Interface
{
    public interface IAnimeRepository
    {
        List<Anime> Animes { get; set; }
        void Save();
    }
}