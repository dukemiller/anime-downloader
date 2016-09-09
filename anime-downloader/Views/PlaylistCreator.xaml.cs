using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using anime_downloader.Classes;
using anime_downloader.Classes.File;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for PlaylistCreator.xaml
    /// </summary>
    public partial class PlaylistCreator
    {
        private readonly Playlist _playlist;

        public PlaylistCreator()
        {
            InitializeComponent();

            if (!File.Exists(Classes.Settings.PlaylistFile))
                OpenButton.Toggle();

            _playlist = MainWindow.Window.Playlist;
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CreateButton.Press();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) => CreateButton.Focus();

        private async void CreateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (MainWindow.Window.CrucialDirectoriesExist())
            {
                _playlist.Refresh();

                if (_playlist.Length == 0)
                {
                    HelperMethods.Alert("No playlist created (no files were found in the episode folders).");
                }

                else
                {

                    if (EpisodeRadio.IsChecked == true)
                        _playlist.OrderByEpisodeNumber();
                    else if (MomentRadio.IsChecked == true)
                        _playlist.OrderByDate();

                    // else pass

                    if (SeparateCheckBox.IsChecked == true)
                        _playlist.SeparateShowOrder();

                    if (ReverseCheckbox.IsChecked == true)
                        _playlist.ReverseOrder();

                    await _playlist.Save();

                    HelperMethods.Alert("Playlist created.");
                }

                MainWindow.Window.Cycle(MainWindow.Window.Playlists);
            }
        }

        private void OpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Classes.Settings.PlaylistFile))
                Process.Start(Classes.Settings.PlaylistFile);
        }
    }
}