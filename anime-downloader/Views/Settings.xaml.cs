using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using anime_downloader.Classes;
using System.Windows.Input;
using anime_downloader.Annotations;
using anime_downloader.Enums;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Settings.xaml
    /// </summary>
    public sealed partial class Settings : INotifyPropertyChanged
    {
        private Classes.Settings _settings;

        private ViewMode _viewMode;

        public Settings()
        {
            InitializeComponent();
        }

        public void New()
        {
            _viewMode = ViewMode.Adding;

            MainWindow.Window.ToggleButtons();

            var path = Path.Combine(Directory.GetCurrentDirectory(), "anime-downloader");

            // Default guessed values
            var settings = new Classes.Settings
            {
                Paths =
                {
                    EpisodeDirectory = Path.Combine(path, "Shows"),
                    WatchedDirectory = Path.Combine(path, "Watched"),
                    TorrentFilesDirectory = Path.Combine(path, "Torrents"),
                    UtorrentFile = @"C:\Program Files (x86)\uTorrent\uTorrent.exe"
                }
            };

            SettingsGrid.DataContext = settings;
            _settings = settings;

            ApplyChangesButton.Content = "Create";
            ApplyChangesButton.Toggle();

        }

        public void Load(Classes.Settings settings)
        {
            _viewMode = ViewMode.Editing;
            _settings = settings;
            SettingsGrid.DataContext = settings;
        }

        private void Textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyChangesButton.Press();
            }
        }

        private void ApplyChangesButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_viewMode == ViewMode.Editing)
                _settings.Save();

            else if (_viewMode == ViewMode.Adding)
            {
                if (EpisodeTextbox.Empty() || TorrentTextbox.Empty() || UtorrentTextbox.Empty())
                    HelperMethods.Alert("You must enter in the episode, torrent files and utorrent path information.");
                else
                {
                    _settings.Save();
                    MainWindow.Window.ToggleButtons();
                    MainWindow.Window.InitializeSettings();
                }
            }

            if (UnsavedChanges)
                UnsavedChanges = false;
        }

        private void AlwaysTrayCheckbox_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Window.Tray.CheckVisibility();
        }

        // Saved changes notification

        private void Textbox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!UnsavedChanges)
                UnsavedChanges = true;
        }

        private void Checkbox_OnClick(object sender, RoutedEventArgs e)
        {
            if (!UnsavedChanges)
                UnsavedChanges = true;
        }
        
        private static bool _unsavedChanges;

        public bool UnsavedChanges
        {
            get { return _unsavedChanges; }
            set
            {
                _unsavedChanges = value;
                OnPropertyChanged(nameof(UnsavedChanges));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }
}