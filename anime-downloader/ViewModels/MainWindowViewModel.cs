using System;
using System.Collections.Generic;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Ioc;
using MessageBox = System.Windows.Forms.MessageBox;

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

            // Etc

            HomeCommand.Execute(1);
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

        public RelayCommand LoadedCommand { get; set; }

        // 

        private void SetCommands()
        {
            LoadedCommand = new RelayCommand(() =>
            {
                CloseCommand = new RelayCommand(Application.Current.MainWindow.Close);
                CreateTray();
            });

            HomeCommand = new RelayCommand(
                () => SelectedIndex = 0,
                () => !Busy
            );

            AnimeCommand = new RelayCommand(
                () => SelectedIndex = 1,
                () => !Busy
            );

            SettingsCommand = new RelayCommand(
                () => SelectedIndex = 2,
                () => !Busy
            );

            DownloadCommand = new RelayCommand(
                () => SelectedIndex = 3,
                () => !Busy
            );

            ManageCommand = new RelayCommand(
                () => SelectedIndex = 4,
                () => !Busy
            );

            PlaylistCreatorCommand = new RelayCommand(
                () => SelectedIndex = 5,
                () => !Busy
            );

            WebCommand = new RelayCommand(
                () => SelectedIndex = 6,
                () => !Busy
            );

            MiscCommand = new RelayCommand(
                () => SelectedIndex = 7,
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