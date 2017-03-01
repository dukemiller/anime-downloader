using System;
using System.Collections.Generic;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool _busy;

        private ViewModelBase _currentView;

        private int _selectedIndex;

        /// <summary>
        ///     Handles logic related to creating and the features of the system tray.
        /// </summary>
        private Tray _tray;

        // 

        public MainWindowViewModel()
        {
            // Commands

            SetCommands();
            ButtonCommands = new[]
            {
                HomeCommand, AnimeCommand, DownloadCommand,
                ManageCommand, MiscCommand, PlaylistCreatorCommand,
                SettingsCommand, WebCommand
            };

            // Messages

            MessengerInstance.Register<WorkMessage>(this, message => Busy = message.Working);
            MessengerInstance.Register<ViewDisplay>(this, ChangeView);

            // Initializations

            CloseCommand = new RelayCommand(Application.Current.MainWindow.Close);
            CurrentView = SimpleIoc.Default.GetInstance<HomeViewModel>();

            // Etc

            HomeCommand.Execute(1);
        }
        
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { Set(() => SelectedIndex, ref _selectedIndex, value); }
        }

        public ViewModelBase CurrentView
        {
            get { return _currentView; }
            set
            {
                CurrentView?.Cleanup();
                Set(() => CurrentView, ref _currentView, value);
            }
        }

        private bool Busy
        {
            get { return _busy; }
            set
            {
                Set(() => Busy, ref _busy, value);
                foreach (var _ in ButtonCommands)
                    _.RaiseCanExecuteChanged();
            }
        }

        // 

        private IEnumerable<RelayCommand> ButtonCommands { get; }

        public RelayCommand CloseCommand { get; set; }

        public RelayCommand HomeCommand { get; set; }

        public RelayCommand AnimeCommand { get; set; }

        public RelayCommand DownloadCommand { get; set; }

        public RelayCommand ManageCommand { get; set; }

        public RelayCommand MiscCommand { get; set; }

        public RelayCommand PlaylistCreatorCommand { get; set; }

        public RelayCommand SettingsCommand { get; set; }

        public RelayCommand WebCommand { get; set; }

        public RelayCommand TrayCommand { get; set; }

        // 

        private void SetCommands()
        {
            TrayCommand = new RelayCommand(CreateTray);

            HomeCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetInstance<HomeViewModel>();
                    SelectedIndex = 1;
                },
                () => !Busy
            );

            AnimeCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetUniqueInstance<AnimeDisplayViewModel>();
                    SelectedIndex = 2;
                },
                () => !Busy
            );

            SettingsCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetInstance<SettingsViewModel>();
                    SelectedIndex = 3;
                },
                () => !Busy
            );

            DownloadCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetUniqueInstance<DownloadViewModel>();
                    SelectedIndex = 4;
                },
                () => !Busy
            );

            ManageCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetUniqueInstance<ManageViewModel>();
                    SelectedIndex = 5;
                },
                () => !Busy
            );

            PlaylistCreatorCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetInstance<PlaylistCreatorViewModel>();
                    SelectedIndex = 6;
                },
                () => !Busy
            );

            WebCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetInstance<WebViewModel>();
                    SelectedIndex = 7;
                },
                () => !Busy
            );

            MiscCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetInstance<MiscViewModel>();
                    SelectedIndex = 8;
                },
                () => !Busy
            );

            // 
        }

        private void ChangeView(ViewDisplay view)
        {
            switch (view)
            {
                case ViewDisplay.Home:
                    HomeCommand.Execute(1);
                    break;

                case ViewDisplay.Anime:
                    AnimeCommand.Execute(1);
                    break;

                case ViewDisplay.Settings:
                    SettingsCommand.Execute(1);
                    break;

                case ViewDisplay.Download:
                    DownloadCommand.Execute(1);
                    break;

                case ViewDisplay.Manage:
                    ManageCommand.Execute(1);
                    break;

                case ViewDisplay.Misc:
                    MiscCommand.Execute(1);
                    break;

                case ViewDisplay.Playlist:
                    PlaylistCreatorCommand.Execute(1);
                    break;

                case ViewDisplay.Web:
                    WebCommand.Execute(1);
                    break;
            }
        }

        private void CreateTray()
        {
            _tray = new Tray(SimpleIoc.Default.GetInstance<ISettingsService>());
        }
    }
}