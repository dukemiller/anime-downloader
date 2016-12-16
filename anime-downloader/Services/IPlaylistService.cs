using System.Threading.Tasks;

namespace anime_downloader.Services
{
    public interface IPlaylistService
    {
        // List 
        int Length { get; }
        void Refresh();

        // List discrimination
        void OrderByEpisodeNumber();
        void OrderByDate();
        void ReverseOrder();
        void SeparateShowOrder();

        // Create the playlist, return path
        Task<string> Create();
    }
}