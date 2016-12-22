using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.Models
{
    public class RadioModel : ViewModelBase
    {
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
        public string ToolTip
        {
            get { return _tooltip; }
            set { Set(() => ToolTip, ref _tooltip, value); }
        }
        #endregion ToolTip

        public string Tag { get; set; }

        public bool Equals(RadioModel obj)
        {
            return Header.Equals(obj.Header);
        }
    }
}
