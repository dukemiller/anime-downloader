namespace anime_downloader.Services
{
    public interface IAnimeAggregateService
    {
        IAnimeService Anime { get; set; }
        IAnimeFileService Files { get; set; }
        IAnimeDownloaderService Downloader { get; set; }
        IMyAnimeListService Mal { get; set; }
    }
}