using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;
using Optional;

namespace anime_downloader.Services.Interfaces
{
    public interface IMyAnimeListApi
    {
        Task<IEnumerable<ProfileAnimeResult>> GetProfile();

        Task<List<FindResult>> FindAsync(string q);

        Task<bool> Login(string username, string password);

        Task<Option<string>> AddAsync(Anime anime, int id);

        Task<Option<string>> UpdateAsync(Anime anime, int id);

        Task<bool> ProfileContains(int id);
    }
}