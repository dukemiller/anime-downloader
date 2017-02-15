using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Services.Interfaces
{
    public interface IMyAnimeListApi
    {
        /// <summary>
        ///     An API call to verify the users given credentials.
        /// </summary>
        /// <returns>
        ///     A boolean stating if the verification was successful.
        /// </returns>
        Task<bool> VerifyCredentialsAsync();

        Task<IEnumerable<ProfileAnimeResult>> GetProfile();

        Task<IEnumerable<FindResult>> FindAsync(string q);

        // 

        Task<HttpContent> GetAsync(string url);

        Task<HttpContent> PostAsync(string url, string data);

        Task<HttpContent> AddAsync(string id, string data);

        Task<HttpContent> UpdateAsync(string id, string data);

        Task<HttpContent> DeleteAsync(string id, string data);
    }
}