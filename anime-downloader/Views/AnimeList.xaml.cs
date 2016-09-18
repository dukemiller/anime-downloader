using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Annotations;
using anime_downloader.Classes;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for AnimeList.xaml
    /// </summary>
    public partial class AnimeList : INotifyPropertyChanged
    {
        private readonly FindPopup _find;

        private Anime SelectedAnime() => DataGrid.SelectedCells.FirstOrDefault().Item as Anime;

        private IEnumerable<Anime> SelectedAnimes() => DataGrid.SelectedCells.Select(c => c.Item).Cast<Anime>().Distinct();

        public ObservableCollection<Anime> Animes { get; set; }

        /// <summary>
        ///     Hoopla to make the data binding in AnimeList
        /// </summary>
        private string _stats;

        public string Stats
        {
            get { return _stats; }
            set
            {
                _stats = value;
                OnPropertyChanged();
            }
        }

        private static string CreateStats()
        {
            var anime = MainWindow.Window.AnimeCollection.Animes.ToList();
            return $"{anime.Count} total. " +
                    $"{anime.Count(a => a.Airing && a.Status.Equals("Watching"))} airing/watching, " +
                    $"{anime.Count(a => a.Status.Equals("Finished"))} finished, " +
                    $"{anime.Count(a => a.Status.Equals("On Hold") || a.Status.Equals("Considering"))} on hold/considering, " +
                    $"{anime.Count(a => a.Status.Equals("Dropped"))} dropped.";
        }
        
        public AnimeList()
        {
            Animes = new ObservableCollection<Anime>(MainWindow.Window.AnimeCollection.FilteredAndSorted());
            Stats = CreateStats();
            InitializeComponent();

            // Clear these set by AnimeDetails && AnimeDetailsMultiple
            KeyDown -= KeyEscapeBack;
            MouseDown -= MouseEscapeBack;
            
            _find = new FindPopup(this);
            FilterComboBox.Text = MainWindow.Window.Settings.FilterBy;
        }

        private void EditAnime()
        {
            if (DataGrid.SelectedCells.Count > 1)
                MainWindow.Window.ChangeDisplay<AnimeDetailsMultiple>().Load(SelectedAnimes().ToList());

            else if (DataGrid.SelectedCells.Count == 1)
                MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(SelectedAnime());
        }

        private void RelegateAnime()
        {
            DeleteProcedure(SelectedAnimes());
            Stats = CreateStats();
            _find.Close();
        }

        private void DeleteProcedure(IEnumerable<Anime> selectedAnime)
        {
            foreach (var anime in selectedAnime)
            {
                if (!anime.Status.Equals("Dropped") &&
                    (anime.MyAnimeList.HasId || anime.IntEpisode() > 0 || anime.HasRating))
                {
                    anime.Status = "Dropped";
                    anime.Airing = false;
                }

                else
                {
                    MainWindow.Window.AnimeCollection.Remove(anime);
                    Animes.Remove(anime);
                }
            }
        }

        // Find
        
        private void FindIcon_OnMouseDown(object sender, MouseButtonEventArgs e) => _find.Toggle();

        // 

        private void UserControl_Loaded(object sender, RoutedEventArgs e) => DataGrid.Focus();

        private void FilterComboBox_OnDropDownClosed(object sender, EventArgs e)
        {
            MainWindow.Window.Settings.FilterBy = FilterComboBox.Text;
            MainWindow.Window.Settings.Save();
            Animes = new ObservableCollection<Anime>(MainWindow.Window.AnimeCollection.FilteredAndSorted());
            _find.Close();
        }

        // 

        public static void KeyEscapeBack(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) // || e.Key == Key.Back)
                MainWindow.Window.Cycle(MainWindow.Window.AnimeList);
        }

        public static void MouseEscapeBack(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton.Equals(MouseButton.XButton1))
                MainWindow.Window.Cycle(MainWindow.Window.AnimeList);
        }

        // Datagrid

        private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
        {
            // there's some problem with sorting the rating, this fixes it
            var column = e.Column;
            var header = column.Header.ToString().ToLower();

            if (header.Equals("rating"))
            {
                if (column.SortDirection == null)
                    Anime.SortedRateFlag = 1;
                else
                    Anime.SortedRateFlag ^= 1;
            }

            // TODO: Separate this from settings? 
            // Autosaving these changes will save changes from the settings window unintentionally
            MainWindow.Window.Settings.SortBy = header.Equals("rating") ? "sortedrating" : header;
            MainWindow.Window.Settings.Flags.SortByReversed = column.SortDirection == ListSortDirection.Ascending;
            MainWindow.Window.Settings.Save();
        }

        private void DataGrid_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                RelegateAnime();
            else if (e.Key == Key.Enter)
                EditAnime();
            else if (e.Key == Key.F && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                _find.Toggle();
            else if (e.Key == Key.OemComma && new[] {Key.LeftCtrl, Key.RightCtrl}.Any(Keyboard.IsKeyDown))
            {
                Clipboard.Clear();
                Clipboard.SetText(string.Join(", ", SelectedAnimes().Select(c => c.Title)));
            }
        }
        
        private void DataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) => EditAnime();

        // Right click Context

        private void Delete_OnClick(object sender, RoutedEventArgs e) => RelegateAnime();

        private async void Search_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedAnime() == null)
                return;
            if (SelectedAnime().MyAnimeList.HasId)
                Process.Start($"http://myanimelist.net/anime/{SelectedAnime().MyAnimeList.Id}");
            else
            {
                MainWindow.Window.ToggleButtons();
                await Classes.Web.MyAnimeList.WebPage.SearchAndOpenAsync(SelectedAnime().Name);
                MainWindow.Window.ToggleButtons();
            }
        }

        private void Edit_OnClick(object sender, RoutedEventArgs e) => MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(SelectedAnime());

        private void Add_OnClick(object sender, RoutedEventArgs e) => MainWindow.Window.ChangeDisplay<AnimeDetails>().New();

        private void AddMultiple_OnClick(object sender, RoutedEventArgs e) => MainWindow.Window.ChangeDisplay<AnimeDetailsMultiple>().New();
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}