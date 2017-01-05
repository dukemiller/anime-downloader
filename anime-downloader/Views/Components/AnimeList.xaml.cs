using System.ComponentModel;
using System.Windows.Controls;
using anime_downloader.Models;
using anime_downloader.ViewModels.Components;

namespace anime_downloader.Views.Components
{
    /// <summary>
    ///     Interaction logic for AnimeList.xaml
    /// </summary>
    public partial class AnimeList
    {
        public AnimeList()
        {
            InitializeComponent();
        }

        private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e)
        {
            // there's some problem with sorting the rating, this fixes it
            var column = e.Column;
            var header = column.Header.ToString().ToLower();

            if (header.Equals("rating"))
                if (column.SortDirection == null)
                    Anime.SortedRateFlag = 1;
                else
                    Anime.SortedRateFlag ^= 1;
            else if (header.Equals("aired"))
                if (column.SortDirection == null)
                    Anime.SortedAiredFlag = 1;
                else
                    Anime.SortedAiredFlag ^= 1;

            // 

            var settings = (DataContext as AnimeListViewModel)?.Settings;
            if (settings != null)
            {
                settings.SortBy = header.Equals("rating")
                    ? "sortedrating"
                    : header.Equals("aired") ? "seasonsort" : header;
                settings.FlagConfig.SortByReversed = column.SortDirection == ListSortDirection.Ascending;
                settings.Save();
            }
        }
    }
}