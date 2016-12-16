using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Models;

namespace anime_downloader.Services
{
    public interface IAnimeFileService
    {

        IEnumerable<AnimeFile> GetEpisodes(EpisodeStatus episodeStatus);
        IEnumerable<AnimeFile> GetEpisodes(Anime anime, EpisodeStatus episodeStatus);
        Task<IEnumerable<AnimeFile>> GetEpisodesAsync(EpisodeStatus episodeStatus);
        Task<IEnumerable<AnimeFile>> GetEpisodesFromAsync(Anime anime, EpisodeStatus episodeStatus);

        AnimeFile FirstEpisode(Anime anime);
        AnimeFile LastEpisode(Anime anime);
    }
}