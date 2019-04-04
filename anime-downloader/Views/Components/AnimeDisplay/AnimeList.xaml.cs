using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using anime_downloader.Classes.Xaml;
using anime_downloader.Models;
using anime_downloader.ViewModels.Components.AnimeDisplay;

namespace anime_downloader.Views.Components.AnimeDisplay
{
    /// <summary>
    ///     Interaction logic for AnimeList.xaml
    /// </summary>
    public partial class AnimeList
    {
        public AnimeList() => InitializeComponent();


        private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;
            var header = column.Header.ToString().ToLower();

            if (header == "rating" || header == "aired in")
            {
                var direction = column.SortDirection != ListSortDirection.Ascending
                    ? ListSortDirection.Ascending
                    : ListSortDirection.Descending;

                column.SortDirection = direction;

                var comparer = header == "rating" 
                    ? (IComparer) new RatingSort(direction) 
                    : new AiringSort(direction);
                var dataView = (ListCollectionView) CollectionViewSource.GetDefaultView(DataGrid.ItemsSource);
                dataView.CustomSort = comparer;
                dataView.Refresh();
                e.Handled = true;
            }

            // 

            var settings = (DataContext as AnimeListViewModel)?.Settings;
            if (settings != null)
            {
                settings.SortBy = header;
                settings.FlagConfig.SortByReversed = column.SortDirection == ListSortDirection.Ascending;
                settings.Save();
            }
        }
    }
}