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

            MessengerInstance.Register<RadioModel>(this, _ =>
            {
                var downloader = SimpleIoc.Default.GetInstance<DownloaderViewModel>();
                CurrentDisplay = downloader;
                downloader.Download(_);
            });

            MessengerInstance.Register<string>(this, _ =>
            {
                if (_.Equals("download_log"))
                {
                    var log = SimpleIoc.Default.GetInstance<DownloadLogViewModel>();
                    CurrentDisplay = log;
                    log.DisplayLogResults();
                }

                else if (_.Equals("tray_download"))
                {
                    var downloader = SimpleIoc.Default.GetInstance<DownloaderViewModel>();
                    CurrentDisplay = downloader;
                    downloader.Download(new RadioModel { Tag = "Next" });
                }
            });
        }

        public ViewModelBase CurrentDisplay
        {
            get { return _currentDisplay; }
            set
            {
                CurrentDisplay?.Cleanup();
                Set(() => CurrentDisplay, ref _currentDisplay, value);
            }
        }
    }
}