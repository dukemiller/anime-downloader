using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.AniList;

namespace anime_downloader.Services.Interfaces
{
    public interface IFindSeasonAnimeService
    {
        Task<IEnumerable<AiringAnime>> New(AnimeSeason season);
        Task<IEnumerable<AiringAnime>> Leftover(AnimeSeason season);
    }
}