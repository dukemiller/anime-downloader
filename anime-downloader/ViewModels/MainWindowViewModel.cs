using System;
using System.Collections.Generic;
using System.Windows;
using anime_downloader.Classes;
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
        private Tray Tray { get; }

        // 

        public MainWindowViewModel()
        {
            Tray = new Tray(SimpleIoc.Default.GetInstance<ISettingsService>());
            CloseCommand = new RelayCommand(Application.Current.MainWindow.Close);
            CurrentView = new HomeViewModel();

            // 

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
                    CurrentView = SimpleIoc.Default.GetInstance<AnimeDisplayViewModel>(Guid.NewGuid().ToString());
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
                    CurrentView = SimpleIoc.Default.GetInstance<DownloadViewModel>(Guid.NewGuid().ToString());
                    SelectedIndex = 4;
                },
                () => !Busy
            );

            ManageCommand = new RelayCommand(
                () =>
                {
                    CurrentView = SimpleIoc.Default.GetInstance<ManageViewModel>(Guid.NewGuid().ToString());
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

            ButtonCommands = new[]
            {
                HomeCommand, AnimeCommand, DownloadCommand,
                ManageCommand, MiscCommand, PlaylistCreatorCommand,
                SettingsCommand, WebCommand
            };

            // 

            MessengerInstance.Register<WorkMessage>(this, _ => { Busy = _.Working; });

            MessengerInstance.Register<Enums.Views>(this, _ =>
            {
                if (_ == Enums.Views.Home)
                    HomeCommand.Execute(1);
                else if (_ == Enums.Views.AnimeDisplay)
                    AnimeCommand.Execute(1);
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

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { Set(() => SelectedIndex, ref _selectedIndex, value); }
        }

        // 

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

        private IEnumerable<RelayCommand> ButtonCommands { get; }

        // 

        public RelayCommand CloseCommand { get; set; }

        public RelayCommand HomeCommand { get; set; }

        public RelayCommand AnimeCommand { get; set; }

        public RelayCommand DownloadCommand { get; set; }

        public RelayCommand ManageCommand { get; set; }

        public RelayCommand MiscCommand { get; set; }

        public RelayCommand PlaylistCreatorCommand { get; set; }

        public RelayCommand SettingsCommand { get; set; }

        public RelayCommand WebCommand { get; set; }
    }
}