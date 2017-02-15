using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Services.Interfaces
{
    /// <summary>
    ///     The logic around using the MyAnimeList API for various tasks. The main
    ///     provided services are to add and update anime on a users animelist and
    ///     to get more detailed information about the users anime.
    /// </summary>
    public interface IMyAnimeListService
    {
        /// <summary>
        ///     Retrieve the closest result on MyAnimeList to a given anime.
        /// </summary>
        /// <returns>
        ///     A MyAnimeList serialized result object.
        /// </returns>
        FindResult ClosestResult(Anime anime, IEnumerable<FindResult> results);

        /// <summary>
        ///     Send an update API request to replace the information about the anime
        ///     on the users MyAnimeList with the given anime.
        /// </summary>
        Task Update(Anime anime);

        /// <summary>
        ///     Send an add API request to add the given anime to the user's MyAnimeList.
        /// </summary>
        Task Add(Anime anime);

        /// <summary>
        ///     Attempt a retrieval on the ID of a given anime and if successful, plant all
        ///     the information of the retrieved MyAnimeList anime onto the Anime object.
        /// </summary>
        /// <returns>
        ///     A boolean stating if the retrieval was successful.
        /// </returns>
        Task<bool> GetId(Anime anime);

        /// <summary>
        ///     Retrieve all results on MyAnimeList matching the query string (searching by anime name).
        /// </summary>
        Task<IEnumerable<FindResult>> Find(string q);

        /// <summary>
        ///     'Refresh' the MyAnimeList details on an anime object, replacing current existing
        ///     information with any information done on a new `Find` on that anime's existing id.
        ///     This operation should only be done on an Anime that already has a MyAnimeList ID, to
        ///     find the ID instead use `GetId(Anime)`.
        /// </summary>
        Task<bool> Refresh(Anime anime);

        /// <summary>
        ///     Gather all animes from the user's profile
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Anime>> GetProfileAnime();

        /// <summary>
        ///     Attempt an indescriminate API update on all of the users anime that is flagged for needing updates.
        /// </summary>
        Task Synchronize();
    }
}