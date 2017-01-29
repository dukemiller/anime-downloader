using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AnimeAggregateService : IAnimeAggregateService
    {
        public AnimeAggregateService(ISettingsService settings)
        {
            AnimeService = new AnimeService(settings);
            FileService = new FileService(settings);
            DownloadService = new NyaaService(settings, AnimeService);
            MalService = new MyAnimeListService(settings, AnimeService);
            PlaylistService = new PlaylistService(settings, FileService);
        }

        public IAnimeService AnimeService { get; set; }
        public IFileService FileService { get; set; }
        public IDownloadService DownloadService { get; set; }
        public IMyAnimeListService MalService { get; set; }
        public IPlaylistService PlaylistService { get; set; }
    }
}