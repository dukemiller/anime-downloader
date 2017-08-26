using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Services.Interfaces
{
    public interface IMyAnimeListApi
    {
        /// <summary>
        ///     An API call to verify the users given credentials.
        /// </summary>
        Task<bool> VerifyCredentialsAsync();

        Task<IEnumerable<ProfileAnimeResult>> GetProfile();

        Task<IEnumerable<FindResult>> FindAsync(string q);

        Task<HttpContent> AddAsync(Anime anime);

        Task<HttpContent> UpdateAsync(Anime anime);
    }
}