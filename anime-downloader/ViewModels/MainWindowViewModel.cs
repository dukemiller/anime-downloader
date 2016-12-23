using System;
using System.Collections.Generic;
using anime_downloader.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace anime_downloader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentView;
        private bool _busy;
        private int _selectedIndex;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { Set(() => SelectedIndex, ref _selectedIndex, value); }
        }

        // 

        public MainWindowViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate, Action close)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;
            Close = close;
            
            // 

            CloseCommand = new RelayCommand(Close);
            CurrentView = new HomeViewModel();

            // 

            HomeCommand = new RelayCommand(
                () => 
                {
                    CurrentView = new HomeViewModel();
                    SelectedIndex = 1;
                },
                () => !Busy
            );

            AnimeListCommand = new RelayCommand(
                () =>
                {
                    CurrentView = new AnimeListViewModel();
                    SelectedIndex = 2;
                },
                () => !Busy
            );

            SettingsCommand = new RelayCommand(
                () =>
                {
                    CurrentView = new SettingsViewModel(Settings);
                    SelectedIndex = 3;
                },
                () => !Busy
            );

            DownloadCommand = new RelayCommand(
                () =>
                {
                    CurrentView = new DownloadViewModel(Settings, AnimeAggregate);
                    SelectedIndex = 4;
                },
                () => !Busy
            );

            ManageCommand = new RelayCommand(
                () =>
                {
                    CurrentView = new ManageViewModel(Settings, AnimeAggregate);
                    SelectedIndex = 5;
                },
                () => !Busy
            );

            PlaylistCreatorCommand = new RelayCommand(
                () =>
                {
                    CurrentView = new PlaylistCreatorViewModel(Settings, AnimeAggregate.Playlist);
                    SelectedIndex = 6;
                },
                () => !Busy
            );

            WebCommand = new RelayCommand(
                () =>
                {
                    CurrentView = new WebViewModel(Settings, AnimeAggregate.Animes, AnimeAggregate.Mal);
                    SelectedIndex = 7;
                },
                () => !Busy
            );

            MiscCommand = new RelayCommand(
                () =>
                {
                    CurrentView = new MiscViewModel(AnimeAggregate.Animes, AnimeAggregate.Files, AnimeAggregate.Mal);
                    SelectedIndex = 8;
                },
                () => !Busy
            );

            // 

            ButtonCommands = new[]
            {
                HomeCommand, AnimeListCommand, DownloadCommand,
                ManageCommand, MiscCommand, PlaylistCreatorCommand,
                SettingsCommand, WebCommand
            };

            // 

            MessengerInstance.Register<WorkMessage>(this, _ =>
            {
                Busy = _.Working;
            });

            MessengerInstance.Register<Enums.Views>(this, _ =>
            {
                if (_ == Enums.Views.Home)
                    HomeCommand.Execute(1);
                else if (_ == Enums.Views.AnimeList)
                    AnimeListCommand.Execute(1);
                else if (_ == Enums.Views.Download)
                    DownloadCommand.Execute(1);
                else if (_ == Enums.Views.Manage)
                    ManageCommand.Execute(1);
                else if (_ == Enums.Views.Misc)
                    MiscCommand.Execute(1);
                else if (_ == Enums.Views.Playlist)
                    PlaylistCreatorCommand.Execute(1);
                else if (_ == Enums.Views.Web)
                    WebCommand.Execute(1);
            });

            HomeCommand.Execute(1);

        }
        
        // 

        private ISettingsService Settings { get; }

        private IAnimeAggregateService AnimeAggregate { get; }

        private Action Close { get; }

        public ViewModelBase CurrentView
        {
            get { return _currentView; }
            set { Set(() => CurrentView, ref _currentView, value); }
        }

        private bool Busy
        {
            get { return _busy; }
            set
            {
                Set(() => Busy, ref _busy, value);
                foreach(var _ in ButtonCommands)
                    _.RaiseCanExecuteChanged();
            }
        }

        private IEnumerable<RelayCommand> ButtonCommands { get; }

        // 

        public RelayCommand CloseCommand { get; set; }

        public RelayCommand HomeCommand { get; set; }

        public RelayCommand AnimeListCommand { get; set; }

        public RelayCommand DownloadCommand { get; set; }

        public RelayCommand ManageCommand { get; set; }

        public RelayCommand MiscCommand { get; set; }

        public RelayCommand PlaylistCreatorCommand { get; set; }

        public RelayCommand SettingsCommand { get; set; }

        public RelayCommand WebCommand { get; set; }
        
    }
}
