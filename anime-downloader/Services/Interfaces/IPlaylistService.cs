using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;

namespace anime_downloader.Services.Interfaces
{
    public interface IPlaylistService
    {
        // List 

        int Length { get; }

        string Path { get; }

        void Refresh();

        void Set(IEnumerable<AnimeFile> files);

        // List discrimination

        void OrderByEpisodeNumber();

        void OrderByDate();

        void ReverseOrder();

        void SeparateShowOrder();

        void AdditionalEpisodesFirst();

        /// <summary>
        ///     Create the playlist.
        /// </summary>
        /// <returns>
        ///     The path to the playlist.
        /// </returns>
        Task<string> Create();
    }
}