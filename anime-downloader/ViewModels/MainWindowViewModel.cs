using System;
using System.Collections.Generic;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Services.Interfaces;
using anime_downloader.ViewModels.Dialogs;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using MaterialDesignThemes.Wpf;

namespace anime_downloader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool _busy;

        private int _selectedIndex;

        /// <summary>
        ///     Handles logic related to creating and the features of the system tray.
        /// </summary>
        private Tray _tray;

        private bool _isShowing;

        // 

        public MainWindowViewModel()
        {
            // Commands

            SetCommands();
            ButtonCommands = new[]
            {
                Home, Anime, Download,
                Discover, Manage, Misc,
                Playlist, Settings, Web
            };

            // Messages

            MessengerInstance.Register<WorkMessage>(this, message => Busy = message.Working);
            MessengerInstance.Register<ViewDisplay>(this, ChangeView);
            MessengerInstance.Register<string>(this, async _ =>
            {
                switch (_)
                {
                    case "loading":
                        if (IsShowing)
                            IsShowing = false;
                        else
                            await DialogHost.Show(new LoadingViewModel());
                        break;
                }
            });

            // Etc

            Home.Execute(1);
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (SelectedIndex == 1 || SelectedIndex == 3)
                    RefreshView();
                Set(() => SelectedIndex, ref _selectedIndex, value);
            }
        }

        /// <summary>
        ///     An unfortunate consequence of the default being to 'reset' some views
        /// </summary>
        public void RefreshView()
        {
            MessengerInstance.Send("reset");
        }

        public bool Busy
        {
            get => _busy;
            set
            {
                Set(() => Busy, ref _busy, value);
                foreach (var _ in ButtonCommands)
                    _.RaiseCanExecuteChanged();
            }
        }

        // 

        public bool IsShowing
        {
            get => _isShowing;
            set => Set(() => IsShowing, ref _isShowing, value);
        }

        private IEnumerable<RelayCommand> ButtonCommands { get; }

        public RelayCommand CloseCommand { get; set; }

        public RelayCommand LoadedCommand { get; set; }

        public RelayCommand Home { get; set; }

        public RelayCommand Anime { get; set; }

        public RelayCommand Download { get; set; }

        public RelayCommand Manage { get; set; }

        public RelayCommand Misc { get; set; }

        public RelayCommand Playlist { get; set; }

        public RelayCommand Settings { get; set; }

        public RelayCommand Web { get; set; }
        
        public RelayCommand Discover { get; set; }

        // 

        private void SetCommands()
        {
            LoadedCommand = new RelayCommand(() =>
            {
                CloseCommand = new RelayCommand(Application.Current.MainWindow.Close);
                CreateTray();
            });

            Home     = new RelayCommand(() => SelectedIndex = 0, () => !Busy);
            Anime    = new RelayCommand(() => SelectedIndex = 1, () => !Busy);
            Discover = new RelayCommand(() => SelectedIndex = 2, () => !Busy);
            Download = new RelayCommand(() => SelectedIndex = 3, () => !Busy);
            Manage   = new RelayCommand(() => SelectedIndex = 4, () => !Busy);
            Playlist = new RelayCommand(() => SelectedIndex = 5, () => !Busy);
            Web      = new RelayCommand(() => SelectedIndex = 6, () => !Busy);
            Settings = new RelayCommand(() => SelectedIndex = 7, () => !Busy);
            Misc     = new RelayCommand(() => SelectedIndex = 8, () => !Busy);

            // 
        }

        private void ChangeView(ViewDisplay view)
        {
            switch (view)
            {
                case ViewDisplay.Home:
                    Home.Execute(1);
                    break;

                case ViewDisplay.Anime:
                    Anime.Execute(1);
                    break;

                case ViewDisplay.Settings:
                    Settings.Execute(1);
                    break;

                case ViewDisplay.Download:
                    Download.Execute(1);
                    break;

                case ViewDisplay.Manage:
                    Manage.Execute(1);
                    break;

                case ViewDisplay.Misc:
                    Misc.Execute(1);
                    break;

                case ViewDisplay.Playlist:
                    Playlist.Execute(1);
                    break;

                case ViewDisplay.Web:
                    Web.Execute(1);
                    break;

                case ViewDisplay.Discover:
                    Discover.Execute(1);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(view), view, null);
            }
        }

        private void CreateTray()
        {
            _tray = new Tray(SimpleIoc.Default.GetInstance<ISettingsService>());
        }
    }
}