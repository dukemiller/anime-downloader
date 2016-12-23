using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AnimeAggregateService: IAnimeAggregateService
    {
        public IAnimeService Animes { get; set; }
        public IAnimeFileService Files { get; set; }
        public IAnimeDownloaderService Downloader { get; set; }
        public IMyAnimeListService Mal { get; set; }
        public IPlaylistService Playlist { get; set; }

        public AnimeAggregateService(ISettingsService settings)
        {
            Animes = new AnimeService(settings);
            Files = new AnimeFileService(settings);
            Downloader = new NyaaService(settings, Files);
            Mal = new MyAnimeListService(settings, Animes);
            Playlist = new PlaylistService(settings, Files);
        }
    }
}
