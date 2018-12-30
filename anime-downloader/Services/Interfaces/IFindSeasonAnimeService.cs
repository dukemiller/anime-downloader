using System;
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
        Task<List<AiringAnime>> New(AnimeSeason animeSeason, Action startLoading);

        /// <summary>
        ///     Retrieve the leftover animes airing from the previous season to the given season
        /// </summary>
        Task<List<AiringAnime>> Leftover(AnimeSeason animeSeason, Action startLoading);

        /// <summary>
        ///     Collect all resources for the airing anime on disk.
        /// </summary>
        Task CollectResources(AiringAnime anime);
    }
}