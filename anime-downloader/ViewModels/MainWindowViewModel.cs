using System;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.ViewModels.Dialogs;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MaterialDesignThemes.Wpf;

namespace anime_downloader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        ///     Handles logic related to creating and the features of the system tray.
        /// </summary>
        private Tray _tray;

        private int _selectedIndex;

        // 

        public MainWindowViewModel()
        {
            MessengerInstance.Register<Display>(this, HandleViewDisplay);
            MessengerInstance.Register<ViewState>(this, HandleViewState);
        }

        // 

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value == _selectedIndex && (value == 1 || value == 3))
                    RefreshView();
                Set(() => SelectedIndex, ref _selectedIndex, value);
            }
        }

        /// <summary>
        ///     An unfortunate consequence of the default being to 'reset' some views
        /// </summary>
        public void RefreshView() => MessengerInstance.Send(ViewRequest.Reset);

        public bool Busy { get; set; }
        
        public bool IsShowing { get; set; }
        
        public RelayCommand CloseCommand => new RelayCommand(Close);

        public RelayCommand LoadedCommand => new RelayCommand(Loaded);

        public RelayCommand Home => new RelayCommand(() => SelectedIndex = 0, () => !Busy);

        public RelayCommand Anime => new RelayCommand(() => SelectedIndex = 1, () => !Busy);

        public RelayCommand Discover => new RelayCommand(() => SelectedIndex = 2, () => !Busy);

        public RelayCommand Download => new RelayCommand(() => SelectedIndex = 3, () => !Busy);

        public RelayCommand Manage => new RelayCommand(() => SelectedIndex = 4, () => !Busy);

        public RelayCommand Playlist => new RelayCommand(() => SelectedIndex = 5, () => !Busy);

        public RelayCommand Web => new RelayCommand(() => SelectedIndex = 6, () => !Busy);

        public RelayCommand Settings => new RelayCommand(() => SelectedIndex = 7, () => !Busy);

        public RelayCommand Misc => new RelayCommand(() => SelectedIndex = 8, () => !Busy);

        // 

        private static void Close() => Application.Current.MainWindow?.Close();

        private void Loaded() => _tray = new Tray();

        private async void HandleViewState(ViewState state)
        {
            switch (state)
            {
                case ViewState.IsLoading:
                    await DialogHost.Show(new LoadingViewModel());
                    break;
                case ViewState.DoneLoading:
                    if (IsShowing)
                        IsShowing = false;
                    break;
                case ViewState.IsWorking:
                    Busy = true;
                    break;
                case ViewState.DoneWorking:
                    Busy = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void HandleViewDisplay(Display view)
        {
            switch (view)
            {
                case Display.Home:
                    Home.Execute(1);
                    break;

                case Display.Anime:
                    Anime.Execute(1);
                    break;

                case Display.Settings:
                    Settings.Execute(1);
                    break;

                case Display.Download:
                    Download.Execute(1);
                    break;

                case Display.Manage:
                    Manage.Execute(1);
                    break;

                case Display.Misc:
                    Misc.Execute(1);
                    break;

                case Display.Playlist:
                    Playlist.Execute(1);
                    break;

                case Display.Web:
                    Web.Execute(1);
                    break;

                case Display.Discover:
                    Discover.Execute(1);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(view), view, null);
            }
        }

    }
}