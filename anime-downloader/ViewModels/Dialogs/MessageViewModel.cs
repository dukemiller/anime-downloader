using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels.Dialogs
{
    public class MessageViewModel: ViewModelBase
    {
        private string _text;

        public string Text
        {
            get => _text;
            set => Set(() => Text, ref _text, value);
        }
    }
}
