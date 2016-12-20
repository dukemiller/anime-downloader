using anime_downloader.Models;
using anime_downloader.Services;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels
{
    public class DownloadViewModel: ViewModelBase
    {
        private ISettingsService Settings { get; }

        private IAnimeAggregateService AnimeAggregate { get; }

        private ViewModelBase _display;

        public ViewModelBase Display
        {
            get { return _display; }
            set { Set(() => Display, ref _display, value); }
        }

        public DownloadViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;

            Display = new DownloadOptionsViewModel();

            MessengerInstance.Register<string>(this, _ =>
            {
                if (_.Equals("download_log"))
                    Display = new DownloadLogViewModel(Settings);
            });

            MessengerInstance.Register<RadioModel>(this, _ =>
            {
                Display = new DownloaderViewModel(Settings, AnimeAggregate, _);
            });
        }
    }
}
