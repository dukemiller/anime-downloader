using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        private string _submitText;

        public string SubmitText
        {
            get { return _submitText; }
            set
            {
                if (value == _submitText) return;
                _submitText = value;
                OnPropertyChanged();
            }
        }

        public void New()
        {
            _viewMode = ViewMode.Adding;

            MainWindow.Window.GetAll<ToggleButton>().Toggle();

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
            SubmitText = "Create";
        }

        public void Load(Classes.Settings settings)
        {
            _viewMode = ViewMode.Editing;
            _settings = settings;
            SubmitText = "Edit";
            SettingsGrid.DataContext = settings;
        }

        private void Textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyChangesButton_OnClick(sender, e);
            }
        }

        private void ApplyChangesButton_OnClick(object sender, RoutedEventArgs e)
        {

            if (_settings.Paths.EpisodeDirectory.Equals("") ||
                _settings.Paths.TorrentFilesDirectory.Equals("") ||
                _settings.Paths.UtorrentFile.Equals(""))
            {
                Methods.Alert("You must enter in the episode, torrent files and utorrent path information.");
                return;
            }

            _settings.Save();

            if (UnsavedChanges)
                UnsavedChanges = false;

            if (_viewMode == ViewMode.Adding)
            {
                MainWindow.Window.GetAll<ToggleButton>().Toggle();
                MainWindow.Window.InitializeSettings();
            }
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