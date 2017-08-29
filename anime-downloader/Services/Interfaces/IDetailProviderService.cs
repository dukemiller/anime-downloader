using System.Threading.Tasks;
using anime_downloader.Models;

namespace anime_downloader.Services.Interfaces
{
    public interface IDetailProviderService
    {
        /// <summary>
        ///     Takes the id needed to fill in service from the anime
        /// </summary>
        int GetId(Anime anime);

        /// <summary>
        ///     Sets the property where the id is with the id value
        /// </summary>
        void SetId(Anime anime, int id);

        /// <summary>
        ///     Find the anime needed for the service (if possible)
        /// </summary>
        Task<(bool successful, int id)> FindId(Anime anime);

        /// <summary>
        ///     Missing details to that anime
        /// </summary>
        Task<(bool successful, bool changesMade)> FillInDetails(Anime anime);
    }
}