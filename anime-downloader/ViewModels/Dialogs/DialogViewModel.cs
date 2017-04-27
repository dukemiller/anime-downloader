using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels.Dialogs
{
    public class DialogViewModel: ViewModelBase
    {
        private string _message;

        public string Message
        {
            get => _message;
            set => Set(() => Message, ref _message, value);
        }
    }
}
