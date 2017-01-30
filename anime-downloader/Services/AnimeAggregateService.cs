using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AnimeAggregateService : IAnimeAggregateService
    {
        public AnimeAggregateService(IAnimeService animeService, 
            IFileService fileService, 
            IDownloadService downloadService, 
            IMyAnimeListService malService, 
            IPlaylistService playlistService)
        {
            AnimeService = animeService;
            FileService = fileService;
            DownloadService = downloadService;
            MalService = malService;
            PlaylistService = playlistService;
        }

        public IAnimeService AnimeService { get; set; }
        public IFileService FileService { get; set; }
        public IDownloadService DownloadService { get; set; }
        public IMyAnimeListService MalService { get; set; }
        public IPlaylistService PlaylistService { get; set; }
    }
}