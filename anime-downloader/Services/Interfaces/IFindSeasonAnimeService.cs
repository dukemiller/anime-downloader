using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.AniList;

namespace anime_downloader.Services.Interfaces
{
    public interface IFindSeasonAnimeService
    {
        /// <summary>
        ///     Retrieve the new animes airing in that season
        /// </summary>
        Task<List<AiringAnime>> New(AnimeSeason season);

        /// <summary>
        ///     Retrieve the leftover animes airing from the previous season to the given season
        /// </summary>
        Task<List<AiringAnime>> Leftover(AnimeSeason season);

        /// <summary>
        ///     Fill in any missing information about the series.
        /// </summary>
        /// <remarks>
        ///     The 'New' and 'Leftover' methods return essentially an AiringAnimeSmall, this will 
        ///     promote their information to be equivalent to an AiringAnime
        /// </remarks>
        Task FillInDetails(AnimeSeason season, bool isNew, AiringAnime anime);
    }
}