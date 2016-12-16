using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Services
{
    public interface IMyAnimeListService
    {
        // Service settings
        NetworkCredential GetCredentials();
        Task<bool> VerifyCredentialsAsync();

        // Api Operations
        Task Update(Anime anime);
        Task Add(Anime anime);
        Task<bool> GetId(Anime anime);

        // Bulk update
        Task Synchronize();

        /*
        Task<List<FindResult>> FindAsync(string q);
        Task<HttpContent> AddAsync(string id, string data);
        Task<HttpContent> UpdateAsync(string id, string data);
        Task<HttpContent> DeleteAsync(string id, string data);
        */
    }
}