using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Annotations;
using anime_downloader.Classes;
using anime_downloader.Classes.File;
using anime_downloader.Enums;
using anime_downloader.Models;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Manage.xaml
    /// </summary>
    public partial class Manage : INotifyPropertyChanged
    {
        private ListBox SelectedListBox => this.GetAll<ListBox>().FirstOrDefault(lb => lb.SelectedItems.Count > 0);

        private List<AnimeFile> SelectedAnimeFiles => SelectedListBox?.SelectedItems.Cast<AnimeFile>().ToList() ?? new List<AnimeFile>();

        private AnimeFile SelectedAnimeFile => SelectedListBox?.SelectedItems.Cast<AnimeFile>().FirstOrDefault();

        public Manage()
        {
            GetData();
            InitializeComponent();
        }

        // 

        private async void GetData()
        {
            _allUnwatched = await MainWindow.Window.AnimeFileCollection.GetEpisodesAsync(EpisodeStatus.Unwatched);
            _allWatched = await MainWindow.Window.AnimeFileCollection.GetEpisodesAsync(EpisodeStatus.Watched);
            WatchedEpisodes = new ObservableCollection<AnimeFile>(_allWatched);
            UnwatchedEpisodes = new ObservableCollection<AnimeFile>(_allUnwatched);
            
        }

        private string _unwatchedLabel;

        public string UnwatchedLabel
        {
            get { return _unwatchedLabel; }
            set
            {
                if (value == _unwatchedLabel) return;
                _unwatchedLabel = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<AnimeFile> UnwatchedEpisodes
        {
            get { return _unwatchedEpisodes; }
            set
            {
                if (Equals(value, _unwatchedEpisodes)) return;
                _unwatchedEpisodes = value;
                OnPropertyChanged();
                UnwatchedLabel = $"({UnwatchedEpisodes.Count} files)";
            }
        }

        public ObservableCollection<AnimeFile> WatchedEpisodes
        {
            get { return _watchedEpisodes; }
            set
            {
                if (Equals(value, _watchedEpisodes)) return;
                _watchedEpisodes = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<AnimeFile> _unwatchedEpisodes;

        private ObservableCollection<AnimeFile> _watchedEpisodes;

        private IEnumerable<AnimeFile> _allUnwatched;

        private IEnumerable<AnimeFile> _allWatched;

        private ListBox _lastSelectedListBox;
        
        // Listbox events

        private void Listbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OpenSelected();
            else if (e.Key == Key.Delete)
                DeleteSelected();
        }

        private void ListBoxItem_GotFocus(object sender, RoutedEventArgs e)
        {
            var parent = (sender as DependencyObject).GetParentUntil<ListBox>();
            if (_lastSelectedListBox != null && !_lastSelectedListBox.Equals(parent))
                _lastSelectedListBox.UnselectAll();
            _lastSelectedListBox = parent;
        }

        // Findbox

        private void FindTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            var textbox = (TextBox) sender;
            var listbox = (ListBox) ((Grid) ((Grid) textbox.Parent).Parent).Children[1];

            if (e.Key == Key.Escape)
                textbox.Clear();

            var q = textbox.Text.ToLower().Trim();

            if (listbox.Name.Equals("Unwatched"))
                UnwatchedEpisodes = SortedFiles(_allUnwatched, q);

            else if (listbox.Name.Equals("Watched"))
                WatchedEpisodes = SortedFiles(_allWatched, q);
        }

        private static ObservableCollection<AnimeFile> SortedFiles(IEnumerable<AnimeFile> files, string q)
        {
            if (q.Equals(""))
                return new ObservableCollection<AnimeFile>(files);

            return new ObservableCollection<AnimeFile>(
                files.Where(a => a.Name.ToLower().Contains(q) || a.Episode.Contains(q))
                    .OrderBy(animeFile => animeFile.Name)
                    .ThenBy(animeFile => animeFile.IntEpisode));
        }

        // 

        private void Button_Move_Click(object sender, RoutedEventArgs e) => MoveSelected();

        // Context menu

        private void Context_Open_Click(object sender, RoutedEventArgs e) => OpenSelected();

        private void Context_Delete_Click(object sender, RoutedEventArgs e) => DeleteSelected();

        private void Context_Move_Click(object sender, RoutedEventArgs e) => MoveSelected();

        private void Context_Profile_Click(object sender, RoutedEventArgs e) => GoToProfileOfSelected();

        private void Context_MouseDoubleClick(object sender, MouseButtonEventArgs e) => PlaySelected();

        // Actions
        
        private void MoveSelected()
        {
            if (SelectedAnimeFiles.Count > 0)
            {
                // Setting up the aliases 
                Action<AnimeFile> remove, addSorted;
                string oldPath, newPath;

                // TODO: find a better way to dynamically reference properties
                if (SelectedListBox.Name.Equals("Unwatched"))
                {
                    oldPath = MainWindow.Window.Settings.Paths.EpisodeDirectory;
                    newPath = MainWindow.Window.Settings.Paths.WatchedDirectory;
                    addSorted = animeFile => WatchedEpisodes.AddSorted(animeFile);
                    remove = animeFile => UnwatchedEpisodes.Remove(animeFile);
                }

                else
                {
                    oldPath = MainWindow.Window.Settings.Paths.WatchedDirectory;
                    newPath = MainWindow.Window.Settings.Paths.EpisodeDirectory;
                    addSorted = animeFile => UnwatchedEpisodes.AddSorted(animeFile);
                    remove = animeFile => WatchedEpisodes.Remove(animeFile);
                }

                // The actual functionality
                if (Directory.Exists(newPath))
                {
                    var movedFiles = EpisodeHandler.MoveAnimeFiles(SelectedAnimeFiles, oldPath, newPath);
                    foreach (var animeFile in movedFiles)
                    {
                        remove(animeFile.Old);
                        addSorted(animeFile.Latest);
                    }
                }

                UnwatchedLabel = $"({UnwatchedEpisodes.Count} files)";
                
            }
        }

        private async void OpenSelected()
        {
            if (SelectedAnimeFiles.Count > 1)
            {
                MainWindow.Window.Playlist.Refresh(SelectedAnimeFiles);
                await MainWindow.Window.Playlist.Save();
                Process.Start(Playlist.PlaylistFile);
            }

            else if (SelectedAnimeFiles.Count == 1)
            {
                Process.Start(SelectedAnimeFiles.First().Path);
            }
        }

        private void DeleteSelected()
        {
            if (SelectedAnimeFiles.Count > 0)
            {
                // TODO: find a better way to dynamically reference properties
                Action<AnimeFile, ListBox> remove = (animeFile, listbox) =>
                {
                    if (listbox.Name.Equals("Unwatched"))
                        UnwatchedEpisodes.Remove(animeFile);
                    else
                        WatchedEpisodes.Remove(animeFile);
                };

                var response = MessageBox.Show(
                    $"Files to be deleted: \n\n{string.Join("\n", SelectedAnimeFiles.Select(ep => ep.Path))}\n\n" +
                    "Are you sure?",
                    "Confirmation",
                    MessageBoxButton.YesNo);

                if (response == MessageBoxResult.Yes)
                {
                    foreach (var episode in SelectedAnimeFiles)
                    {
                        File.Delete(episode.Path);
                        remove(episode, SelectedListBox);
                    }

                    UnwatchedLabel = $"({UnwatchedEpisodes.Count} files)";
                }
            }
        }

        private void GoToProfileOfSelected()
        {
            if (SelectedAnimeFile != null)
            {
                var anime = Anime.Closest.To(SelectedAnimeFile, MainWindow.Window.Settings);
                if (anime != null)
                    MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(anime);
                else
                    Methods.Alert($"No anime profile found for {SelectedAnimeFile.Name}.");
            }
        }

        private void PlaySelected()
        {
            if (SelectedAnimeFile != null)
                Process.Start(SelectedAnimeFile.Path);
        }

        // Label

        private void Unwatched_Label_OnMouseUp(object sender, MouseButtonEventArgs e) => Process.Start(MainWindow.Window.Settings.Paths.EpisodeDirectory);

        private void Watched_Label_OnMouseUp(object sender, MouseButtonEventArgs e) => Process.Start(MainWindow.Window.Settings.Paths.WatchedDirectory);

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
}