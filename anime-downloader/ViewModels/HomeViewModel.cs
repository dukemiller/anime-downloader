using System.Reflection;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private string _version;

        public HomeViewModel()
        {
            Version = Assembly.GetExecutingAssembly()
                .GetName()
                .Version
                .ToString();
        }

        public string Version
        {
            get { return _version; }
            set { Set(() => Version, ref _version, value); }
        }
    }
}