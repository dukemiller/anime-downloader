using System.Windows.Controls;
using anime_downloader.Classes;

namespace anime_downloader.Views
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
            var col = e.Column;
            if (col.Header.Equals("Rating"))
            {
                if (col.SortDirection == null)
                    Anime.SortedRateFlag = 1;
                else
                    Anime.SortedRateFlag ^= 1;
            }
        }
    }
}