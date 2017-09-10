using System.Threading.Tasks;
using anime_downloader.Models;

namespace anime_downloader.Services.Interfaces
{
    public interface IDetailProviderService: IRequireIdentification
    {
        /// <summary>
        ///     Missing details to that anime
        /// </summary>
        Task<(bool successful, bool changesMade)> FillInDetails(Anime anime);

        /// <summary>
        ///     Check and fill in information about series continuation episode counts.
        /// </summary>
        Task<bool> CheckSeriesContinuation(Anime anime);
    }
}