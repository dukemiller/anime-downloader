using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.ViewModels.Components;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.ViewModels.Displays
{
    public class HomeViewModel : ViewModelBase
    {
        public HomeViewModel()
        {
            VersionManager.NeedsUpdateChanged += OnChanged;
            MessengerInstance.Register<Request>(this, HandleRequest);
        }

        // 

        public bool NeedsUpdate { get; set; } = VersionManager.NeedsUpdate;

        public string Version { get; set; } = VersionManager.Version;

        public ViewModelBase Notes { get; set; } = SimpleIoc.Default.GetInstance<NotesViewModel>();

        public RelayCommand UpdateCommand => new RelayCommand(Update);
        
        // 

        private async void Update()
        {
            try
            {
                MessengerInstance.Send(ViewState.IsWorking);
                await VersionManager.Update();
            }
            catch
            {
                MessengerInstance.Send(ViewState.DoneWorking);
                Methods.Alert("Update was unsuccessful.");
            }
        }

        private static async void HandleRequest(Request request)
        {
            if (request == Request.CheckForUpdates)
                await VersionManager.Check();
        }

        private void OnChanged(object sender, bool needsUpdate) => NeedsUpdate = needsUpdate;

    }
}