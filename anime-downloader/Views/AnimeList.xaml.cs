using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Classes;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for AnimeList.xaml
    /// </summary>
    public partial class AnimeList
    {
        private readonly FindPopup _find;

        private Anime SelectedAnime() => DataGrid.SelectedCells.FirstOrDefault().Item as Anime;

        private IEnumerable<Anime> SelectedAnimes() => DataGrid.SelectedCells.Select(c => c.Item).Cast<Anime>().Distinct();

        public AnimeList()
        {
            InitializeComponent();
            this.Refresh(MainWindow.Window.AnimeCollection);

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
            _find.Close();
            this.Refresh(MainWindow.Window.AnimeCollection);
        }

        private static void DeleteProcedure(IEnumerable<Anime> selectedAnime)
        {
            foreach (var anime in selectedAnime)
            {
                if (!anime.Status.Equals("Dropped") && (anime.MyAnimeList.HasId || anime.IntEpisode() > 0 || anime.HasRating))
                    anime.Status = "Dropped";
                else
                    MainWindow.Window.AnimeCollection.Remove(anime);
            }
        }

        // Find

        private void FindIcon_OnMouseEnter(object sender, MouseEventArgs e) => FindIcon.Opacity = 0.8;

        private void FindIcon_OnMouseLeave(object sender, MouseEventArgs e) => FindIcon.Opacity = 1.0;

        private void FindIcon_OnMouseDown(object sender, MouseButtonEventArgs e) => _find.Toggle();

        // 

        private void UserControl_Loaded(object sender, RoutedEventArgs e) => DataGrid.Focus();

        private void FilterComboBox_OnDropDownClosed(object sender, EventArgs e)
        {
            MainWindow.Window.Settings.FilterBy = FilterComboBox.Text;
            this.Refresh(MainWindow.Window.AnimeCollection);
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

            MainWindow.Window.Settings.SortBy = header.Equals("rating") ? "sortedrating" : header;
            MainWindow.Window.Settings.Flags.SortByReversed = column.SortDirection == ListSortDirection.Ascending;
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
    }
}