using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels.Components
{
    public class DialogViewModel: ViewModelBase
    {
        private string _message;

        public string Message
        {
            get { return _message; }
            set { Set(() => Message, ref _message, value); }
        }
    }
}
