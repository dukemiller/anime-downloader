using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.ViewModels
{
    public class DownloadViewModel : ViewModelBase
    {
        private ViewModelBase _currentDisplay;

        // 

        public DownloadViewModel()
        {
            // Default display

            CurrentDisplay = SimpleIoc.Default.GetInstance<DownloadOptionsViewModel>();

            // Messages

            MessengerInstance.Register<RadioModel<DownloadOption>>(this, _ =>
            {
                var downloader = SimpleIoc.Default.GetUniqueInstance<DownloaderViewModel>();
                CurrentDisplay = downloader;
                downloader.Download(_);
            });

            MessengerInstance.Register<string>(this, _ =>
            {
                switch (_)
                {
                    case "download_log":
                        var log = SimpleIoc.Default.GetUniqueInstance<DownloadLogViewModel>();
                        CurrentDisplay = log;
                        log.DisplayLogResults();
                        break;
                    case "reset":
                        CurrentDisplay = SimpleIoc.Default.GetInstance<DownloadOptionsViewModel>();
                        break;
                }
            });
        }

        public ViewModelBase CurrentDisplay
        {
            get => _currentDisplay;
            set
            {
                CurrentDisplay?.Cleanup();
                Set(() => CurrentDisplay, ref _currentDisplay, value);
            }
        }
    }
}