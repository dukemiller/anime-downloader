using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;
using Optional;

namespace anime_downloader.Services.Interfaces
{
    /// <summary>
    ///     The logic around using a remote service for synchronizing your list to theirs. 
    /// </summary>
    public interface ISyncProviderService: IRequireIdentification
    {
        /// <summary>
        ///     Send an update API request to replace the information about the anime
        ///     on the users list with the given anime.
        /// </summary>
        Task Update(Anime anime);

        /// <summary>
        ///     Send an add API request to add the given anime to the users list.
        /// </summary>
        Task Add(Anime anime);

        /// <summary>
        ///     Attempt an indescriminate API update on all of the users anime that is flagged for needing updates.
        /// </summary>
        Task Synchronize();

        /// <summary>
        ///     Gather all animes from the user's profile
        /// </summary>
        Task<IEnumerable<Anime>> LoadProfile();

        /// <summary>
        ///     Search for the show by querying the page and scraping the results from the DOM.
        /// </summary>
        /// <returns>If a result is found, returns the url for the page, otherwise null.</returns>
        Task<Option<string>> FindProfilePage(string text);
    }
}