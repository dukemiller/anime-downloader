using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AnimeAggregateService : IAnimeAggregateService
    {
        public AnimeAggregateService(ISettingsService settings)
        {
            AnimeService = new AnimeService(settings);
            FileService = new AnimeFileService(settings);
            DownloadService = new NyaaService(settings, FileService, AnimeService);
            MalService = new MyAnimeListService(settings, AnimeService);
            PlaylistService = new PlaylistService(settings, FileService);
        }

        public IAnimeService AnimeService { get; set; }
        public IAnimeFileService FileService { get; set; }
        public IAnimeDownloaderService DownloadService { get; set; }
        public IMyAnimeListService MalService { get; set; }
        public IPlaylistService PlaylistService { get; set; }
    }
}