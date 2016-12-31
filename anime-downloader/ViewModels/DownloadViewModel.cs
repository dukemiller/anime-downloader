using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels
{
    public class DownloadViewModel : ViewModelBase
    {
        private ViewModelBase _display;

        public DownloadViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;

            Display = new DownloadOptionsViewModel();

            MessengerInstance.Register<string>(this, _ =>
            {
                if (_.Equals("download_log"))
                    Display = new DownloadLogViewModel(Settings);

                else if (_.Equals("tray_download"))
                    Display = new DownloaderViewModel(Settings, AnimeAggregate, new RadioModel {Tag = "Next"});
            });

            MessengerInstance.Register<RadioModel>(this,
                _ => { Display = new DownloaderViewModel(Settings, AnimeAggregate, _); });
        }

        private ISettingsService Settings { get; }

        private IAnimeAggregateService AnimeAggregate { get; }

        public ViewModelBase Display
        {
            get { return _display; }
            set { Set(() => Display, ref _display, value); }
        }
    }
}