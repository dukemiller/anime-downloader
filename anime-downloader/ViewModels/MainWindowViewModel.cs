using System;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace anime_downloader.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentView;
        private string _currentlyChecked;

        public MainWindowViewModel(Action close)
        {
            Close = close;

            // 

            CloseCommand = new RelayCommand(Close);
            CurrentView = new HomeViewModel();

            // 

            HomeCommand = new RelayCommand(() => CurrentView = new HomeViewModel());
            AnimeListCommand = new RelayCommand(() => CurrentView = new AnimeListViewModel());
            AnimeDetailsCommand = new RelayCommand(() => CurrentView = new AnimeDetailsViewModel());
            AnimeDetailsMultipleCommand = new RelayCommand(() => CurrentView = new AnimeDetailsMultipleViewModel());
            DownloaderCommand = new RelayCommand(() => CurrentView = new DownloaderViewModel());
            DownloadOptionsCommand = new RelayCommand(() => CurrentView = new DownloadOptionsViewModel());
            ManageCommand = new RelayCommand(() => CurrentView = new ManageViewModel());
            MiscCommand = new RelayCommand(() => CurrentView = new MiscViewModel());
            PlaylistCreatorCommand = new RelayCommand(() => CurrentView = new PlaylistCreatorViewModel());
            SettingsCommand = new RelayCommand(() => CurrentView = new SettingsViewModel());
            WebCommand = new RelayCommand(() => CurrentView = new WebViewModel());
        }

        // 
        
        public Action Close { get; set; }

        public string CurrentlyChecked
        {
            get { return _currentlyChecked; }
            set
            {
                _currentlyChecked = value;
                RaisePropertyChanged();
            }
        }

        public ViewModelBase CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                RaisePropertyChanged();
            }
        }

        // 

        public ICommand CloseCommand { get; set; }

        public ICommand HomeCommand { get; set; }

        public ICommand AnimeListCommand { get; set; }

        public ICommand AnimeDetailsCommand { get; set; }

        public ICommand AnimeDetailsMultipleCommand { get; set; }

        public ICommand DownloaderCommand { get; set; }

        public ICommand DownloadOptionsCommand { get; set; }

        public ICommand ManageCommand { get; set; }

        public ICommand MiscCommand { get; set; }

        public ICommand PlaylistCreatorCommand { get; set; }

        public ICommand SettingsCommand { get; set; }

        public ICommand WebCommand { get; set; }


    }
}
