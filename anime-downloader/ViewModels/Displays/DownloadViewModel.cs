using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.ViewModels.Components.Download;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.ViewModels.Displays
{
    public class DownloadViewModel : ViewModelBase
    {
        private ViewModelBase _currentDisplay;

        // 

        public DownloadViewModel()
        {
            // Default display
            CurrentDisplay = SimpleIoc.Default.GetInstance<OptionsViewModel>();

            // Messages
            MessengerInstance.Register<RadioModel<DownloadOption>>(this, HandleRadioModel);
            MessengerInstance.Register<Component>(this, HandleComponent);
            MessengerInstance.Register<ViewRequest>(this, HandleViewAction);
        }

        // 
        
        public ViewModelBase CurrentDisplay
        {
            get => _currentDisplay;
            set
            {
                CurrentDisplay?.Cleanup();
                Set(() => CurrentDisplay, ref _currentDisplay, value);
            }
        }

        // 

        private void HandleRadioModel(RadioModel<DownloadOption> _)
        {
            var downloader = SimpleIoc.Default.GetUniqueInstance<OutputViewModel>();
            CurrentDisplay = downloader;
            downloader.Download(_);
        }

        private void HandleViewAction(ViewRequest _)
        {
            switch (_)
            {
                case ViewRequest.Reset:
                    CurrentDisplay = SimpleIoc.Default.GetInstance<OptionsViewModel>();
                    break;
            }
        }

        private void HandleComponent(Component _)
        {
            switch (_)
            {
                case Component.Log:
                    var log = SimpleIoc.Default.GetUniqueInstance<LogViewModel>();
                    CurrentDisplay = log;
                    log.DisplayLogResults();
                    break;
            }
        }
    }
}