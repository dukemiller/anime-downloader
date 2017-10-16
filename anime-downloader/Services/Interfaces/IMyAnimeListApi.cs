using System.Collections.Generic;
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

        Task<List<FindResult>> FindAsync(string q);

        Task<(bool successful, string content)> AddAsync(Anime anime, int id);

        Task<(bool successful, string content)> UpdateAsync(Anime anime, int id);
    }
}