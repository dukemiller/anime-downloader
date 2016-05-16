using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using anime_downloader.Classes.File;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Manage.xaml
    /// </summary>
    public partial class Manage
    {
        public Manage()
        {
            InitializeComponent();
        }

        public Playlist Playlist { get; set; }

        public IEnumerable<AnimeEpisode> Unwatched { get; set; }

        public IEnumerable<AnimeEpisode> Watched { get; set; }

        public bool WatchedExists { get; set; }

        public bool UnwatchedExists { get; set; }

        private void UnwatchedFindTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                UnwatchedFindTextBox.Clear();
                UnwatchedList.ItemsSource = Unwatched;
            }
            else
            {
                var q = UnwatchedFindTextBox.Text.ToLower().Trim();
                var result = Unwatched.Where(a => a.Name.ToLower().Contains(q) || a.Episode.Contains(q));
                UnwatchedList.ItemsSource = result;
            }
        }

        private void WatchedFindTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                WatchedFindTextBox.Clear();
                WatchedList.ItemsSource = Watched;
            }
            else
            {
                var q = WatchedFindTextBox.Text.ToLower().Trim();
                var result = Watched.Where(a => a.Name.ToLower().Contains(q) || a.Episode.Contains(q));
                WatchedList.ItemsSource = result;
            }
        }

        private void MouseEnterItem(object sender, MouseEventArgs e)
        {
            ((Shape) sender).Opacity = 0.6;
        }

        private void MouseLeaveItem(object sender, MouseEventArgs e)
        {
            ((Shape) sender).Opacity = 1.0;
        }

        private void ContextOpen_Click(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox ?? (ListBox) ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget;

            var episodes = listBox.SelectedItems.Cast<AnimeEpisode>().ToList();

            if (episodes.Count > 1)
            {
                Playlist.Refresh(episodes);
                Playlist.Save();
                Process.Start(Playlist.PlaylistFile);
            }

            else if (episodes.Count == 1)
            {
                Process.Start(episodes.First().FilePath);
            }
        }

        private void ContextDelete_Click(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox ?? (ListBox) ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget;

            var episodes = listBox.SelectedItems.Cast<AnimeEpisode>().ToList();

            if (episodes.Count > 0)
            {
                var response =
                    MessageBox.Show(
                        $"Files to be deleted: \n\n{string.Join("\n", episodes.Select(ep => ep.FilePath))}\n\n" +
                        "Are you sure?",
                        "Confirmation",
                        MessageBoxButton.YesNo);

                if (response == MessageBoxResult.Yes)
                    foreach (var episode in episodes)
                        File.Delete(episode.FilePath);
            }
        }

        private void Context_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = ((ListBox) sender).SelectedItem;
            if (selected != null)
            {
                Process.Start(((AnimeEpisode) selected).FilePath);
            }
        }

        private void Listbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ContextOpen_Click(sender, e);
            else if (e.Key == Key.Delete)
                ContextDelete_Click(sender, e);
        }
    }
}