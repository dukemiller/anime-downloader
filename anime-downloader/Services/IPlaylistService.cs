using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;

namespace anime_downloader.Services
{
    public interface IPlaylistService
    {
        // List 
        int Length { get; }
        void Refresh();
        void Set(IEnumerable<AnimeFile> files);
        string Path { get; }

        // List discrimination
        void OrderByEpisodeNumber();
        void OrderByDate();
        void ReverseOrder();
        void SeparateShowOrder();

        // Create the playlist, return path
        Task<string> Create();
    }
}