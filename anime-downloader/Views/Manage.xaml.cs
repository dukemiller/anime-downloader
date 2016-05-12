using System.Collections;
using System.Windows.Input;
using anime_downloader.Classes.File;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace anime_downloader.Views
{
    /// <summary>
    /// Interaction logic for Manage.xaml
    /// </summary>
    public partial class Manage
    {
        public Playlist Playlist { get; set; }

        public IEnumerable<AnimeEpisode> Unwatched { get; set; }

        public IEnumerable<AnimeEpisode> Watched { get; set; }

        public Manage()
        {
            InitializeComponent();
        }

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

        private void WatchedOpen_Click(object sender, RoutedEventArgs e)
        {
            var episodes = WatchedList.SelectedItems.Cast<AnimeEpisode>().ToList();
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

        private void UnwatchedOpen_Click(object sender, RoutedEventArgs e)
        {
            var episodes = UnwatchedList.SelectedItems.Cast<AnimeEpisode>().ToList();
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

        private void WatchedDelete_Click(object sender, RoutedEventArgs e)
        {
            var episodes = WatchedList.SelectedItems.Cast<AnimeEpisode>().ToList();

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

        private void UnwatchedDelete_Click(object sender, RoutedEventArgs e)
        {
            var episodes = UnwatchedList.SelectedItems.Cast<AnimeEpisode>().ToList();

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

        private void MoveRight_MouseEnter(object sender, MouseEventArgs e)
        {
            MoveRight.Opacity = 0.6;
        }

        private void MoveRight_MouseLeave(object sender, MouseEventArgs e)
        {
            MoveRight.Opacity = 1;
        }

        private void MoveLeft_MouseEnter(object sender, MouseEventArgs e)
        {
            MoveLeft.Opacity = 0.6;
        }

        private void MoveLeft_MouseLeave(object sender, MouseEventArgs e)
        {
            MoveLeft.Opacity = 1;
        }

        private void UnwatchedList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = UnwatchedList.SelectedItem;
            if (selected != null)
            {
                Process.Start(((AnimeEpisode) selected).FilePath);
            }
        }

        private void WatchedList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = WatchedList.SelectedItem;
            if (selected != null)
            {
                Process.Start(((AnimeEpisode) selected).FilePath);
            }
        }
    }
}
