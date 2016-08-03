using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using anime_downloader.Classes;
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

        private ObservableCollection<AnimeFile> Watched { get; set; }

        private ObservableCollection<AnimeFile> Unwatched { get; set; }

        private ObservableCollection<AnimeFile> UnwatchedWindow
        {
            get { return _watchedWindow; }

            set
            {
                _watchedWindow = value;
                UnwatchedFilesLabel.Content = $"({value.Count} files)";
                UnwatchedList.ItemsSource = value;
                UnwatchedList.Items.Refresh();
            }
        }

        private ObservableCollection<AnimeFile> WatchedWindow
        {
            get { return _unwatchedWindow; }

            set
            {
                _unwatchedWindow = value;
                WatchedList.ItemsSource = value;
                WatchedList.Items.Refresh();
            }
        }

        private ObservableCollection<AnimeFile> _watchedWindow;

        private ObservableCollection<AnimeFile> _unwatchedWindow;

        private Classes.Settings _settings;

        private MainWindow _mainWindow;

        public void SetInitialValues(
            MainWindow mainWindow,
            ObservableCollection<AnimeFile> unwatched,
            ObservableCollection<AnimeFile> watched,
            Classes.Settings settings)
        {
            _mainWindow = mainWindow;   // TODO: I REALLY dont like this
            _settings = settings;

            Unwatched = unwatched;
            Watched = watched;

            UnwatchedWindow = new ObservableCollection<AnimeFile>(unwatched);
            WatchedWindow = new ObservableCollection<AnimeFile>(watched);
        }

        public Playlist Playlist { private get; set; }
        
        private void FindTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            var textbox = (TextBox) sender;
            var listbox = ((
                textbox                   // textbox
                .Parent as Grid)?         // panel it's contained in
                .Parent as Grid)?         // entire grid
                .Children[1] as ListBox;  // element that contains the listbox

            ObservableCollection<AnimeFile> current;
            Action<ObservableCollection<AnimeFile>> currentWindowSetter;

            if (listbox != null && listbox.Name.Equals(nameof(UnwatchedList)))
            {
                current = Unwatched;
                currentWindowSetter = v => UnwatchedWindow = v;
            }

            else
            {
                current = Watched;
                currentWindowSetter = v => WatchedWindow = v;
            }

            if (e.Key == Key.Escape)
            {
                textbox.Clear();
                currentWindowSetter(current);
            }

            else
            {
                var q = textbox.Text.ToLower().Trim();
                var result = new ObservableCollection<AnimeFile>(current
                    .Where(a => a.Name.ToLower().Contains(q) || a.Episode.Contains(q))
                    .OrderBy(animeFile => animeFile.Name)
                    .ThenBy(animeFile => animeFile.IntEpisode));
                currentWindowSetter(result);
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

        private void Button_Move_Click(object sender, RoutedEventArgs e)
        {
            var listbox = (((((
                sender as Rectangle)?       // button
                .Parent as DockPanel)?      // panel it's contained in
                .Parent as Grid)?           // grid half of the bottom panel
                .Parent as Grid)?           // entire grid
                .Parent as Grid)?           // parent to the grid
                .Children[1] as ListBox;    // element that contains the listbox
            Context_Move_Click(listbox, e);
        }

        private async void Context_Open_Click(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox ?? (ListBox) ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget;
            var episodes = listBox.SelectedItems.Cast<AnimeFile>().ToList();

            if (episodes.Count > 1)
            {
                Playlist.Refresh(episodes);
                await Playlist.Save();
                Process.Start(Playlist.PlaylistFile);
            }

            else if (episodes.Count == 1)
            {
                Process.Start(episodes.First().Path);
            }
        }

        private void Context_Delete_Click(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox ?? (ListBox) ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget;
            var episodes = listBox.SelectedItems.Cast<AnimeFile>().ToList();
            var details = new ListDetails(listBox, this);

            if (episodes.Count > 0)
            {
                var response = MessageBox.Show(
                    $"Files to be deleted: \n\n{string.Join("\n", episodes.Select(ep => ep.Path))}\n\n Are you sure?",
                    "Confirmation",
                    MessageBoxButton.YesNo);

                if (response == MessageBoxResult.Yes)
                {
                    foreach (var episode in episodes)
                    {
                        File.Delete(episode.Path);
                        details.Current.Remove(episode);
                        details.CurrentWindow.Remove(episode);
                    }

                    UnwatchedFilesLabel.Content = $"({UnwatchedWindow.Count} files)";
                }
            }
        }

        private void Context_Move_Click(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox ?? (ListBox) ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget;
            var details = new TransferDetails(listBox, this, _settings);

            if (details.OppositeFolderExists)
            {
                var episodes = listBox.SelectedItems.Cast<AnimeFile>().ToList();
                if (episodes.Count >= 1)
                {
                    var episodePair = EpisodeHandler.MoveEpisodesToDestination(listBox,
                        details.CurrentPath,
                        details.Otherpath
                    );

                    foreach (var pair in episodePair)
                    {
                        var oldEpisode = pair.Item1;
                        var newEpisode = pair.Item2;

                        details.Details.Current.Remove(oldEpisode);
                        details.Details.CurrentWindow.Remove(oldEpisode);
                        details.Details.Other.AddSorted(newEpisode);
                        details.Details.OtherWindow.AddSorted(newEpisode);
                    }

                    UnwatchedFilesLabel.Content = $"({Unwatched.Count} files)";
                }
            }
        }

        /// <summary>
        ///     TODO: I REALLY dislike how this is done
        /// </summary>
        private void Context_Profile_Click(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox ?? (ListBox) ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget;
            var episode = listBox.SelectedItems.Cast<AnimeFile>().FirstOrDefault();
            if (episode != null)
            {
                var anime = Anime.Closest.To(episode, _settings);
                if (anime != null)
                    _mainWindow.AnimeDetails_Single(anime);
                else
                    MainWindow.Alert($"No anime profile found for {episode.Name}.");
            }
        }

        private void Context_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = ((ListBox) sender).SelectedItem;

            if (selected != null)
            {
                Process.Start(((AnimeFile) selected).Path);
            }
        }

        private void Listbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Context_Open_Click(sender, e);
            else if (e.Key == Key.Delete)
                Context_Delete_Click(sender, e);
        }

        private class TransferDetails
        {
            public bool OppositeFolderExists { get; }

            public string CurrentPath { get; }

            public string Otherpath { get; }

            public ListDetails Details { get; }
            
            public TransferDetails(IFrameworkInputElement listbox, Manage manage, Classes.Settings settings)
            {
                Details = new ListDetails(listbox, manage);

                if (listbox.Name.Equals(nameof(UnwatchedList)))
                {
                    OppositeFolderExists = Directory.Exists(settings.Paths.WatchedDirectory);
                    CurrentPath = settings.Paths.EpisodeDirectory;
                    Otherpath = settings.Paths.WatchedDirectory;
                }

                else
                {
                    OppositeFolderExists = Directory.Exists(settings.Paths.EpisodeDirectory);
                    CurrentPath = settings.Paths.WatchedDirectory;
                    Otherpath = settings.Paths.EpisodeDirectory;
                }
            }
        }

        private class ListDetails
        {
            public ObservableCollection<AnimeFile> Current { get; }

            public ObservableCollection<AnimeFile> CurrentWindow { get; }

            public ObservableCollection<AnimeFile> Other { get; }

            public ObservableCollection<AnimeFile> OtherWindow { get; }

            public ListDetails(IFrameworkInputElement listbox, Manage window)
            {
                if (listbox.Name.Equals(nameof(UnwatchedList)))
                {
                    Current = window.Unwatched;
                    CurrentWindow = window.UnwatchedWindow;
                    Other = window.Watched;
                    OtherWindow = window.WatchedWindow;
                }
                else
                {
                    Current = window.Watched;
                    CurrentWindow = window.WatchedWindow;
                    Other = window.Unwatched;
                    OtherWindow = window.UnwatchedWindow;
                }
            }
        }
        
    }
}