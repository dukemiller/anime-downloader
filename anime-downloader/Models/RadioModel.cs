using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    public class RadioModel<T> : ViewModelBase
    {
        public string Tag { get; set; }

        public bool Equals(RadioModel<T> obj)
        {
            return Header.Equals(obj.Header);
        }

        #region Header

        private string _header = string.Empty;

        public string Header
        {
            get { return _header; }
            set { Set(() => Header, ref _header, value); }
        }

        #endregion Header

        #region ToolTip

        private string _tooltip = string.Empty;
        private T _data;

        public string ToolTip
        {
            get { return _tooltip; }
            set { Set(() => ToolTip, ref _tooltip, value); }
        }

        #endregion ToolTip

        public T Data
        {
            get { return _data; }
            set { Set(() => Data, ref _data, value); }
        }
    }

    public class RadioModel : RadioModel<object>
    {
        
    }
}