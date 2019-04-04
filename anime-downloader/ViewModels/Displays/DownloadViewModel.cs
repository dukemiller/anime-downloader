using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.ViewModels.Components.Download;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.ViewModels.Displays
{
    public class DownloadViewModel : ViewModelBase
    {
        private static OutputViewModel Output => SimpleIoc.Default.GetInstance<OutputViewModel>();

        private static OptionsViewModel Options => SimpleIoc.Default.GetInstance<OptionsViewModel>();

        // 

        public DownloadViewModel()
        {
            MessengerInstance.Register<Radio<DownloadOption>>(this, HandleRadioModel);
            MessengerInstance.Register<Component>(this, HandleComponent);
            MessengerInstance.Register<ViewRequest>(this, HandleViewRequest);
        }

        // 

        public ViewModelBase CurrentDisplay { get; set; } = Options;

        // 

        private async void HandleRadioModel(Radio<DownloadOption> option)
        {
            CurrentDisplay = Output;
            await Output.Download(option);
        }

        private void HandleViewRequest(ViewRequest _)
        {
            if (_ == ViewRequest.Reset)
                CurrentDisplay = Options;
        }

        private async void HandleComponent(Component _)
        {
            if (_ != Component.Log)
                return;

            CurrentDisplay = Output;
            await Output.Log();
        }
    }
}