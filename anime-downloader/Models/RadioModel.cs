using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    public class RadioModel<T> : ViewModelBase
    {
        private T _data;

        private string _header = string.Empty;

        private string _tooltip = string.Empty;

        public string Header
        {
            get => _header;
            set => Set(() => Header, ref _header, value);
        }

        public string ToolTip
        {
            get => _tooltip;
            set => Set(() => ToolTip, ref _tooltip, value);
        }

        public T Data
        {
            get => _data;
            set => Set(() => Data, ref _data, value);
        }
    }

    public abstract class RadioModel: RadioModel<string> { }
}