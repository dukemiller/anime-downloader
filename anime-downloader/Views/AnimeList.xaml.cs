using anime_downloader.Classes;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void FindRectangle_OnMouseEnter(object sender, MouseEventArgs e)
        {
            FindRectangle.Opacity = 0.8;
        }

        private void FindRectangle_OnMouseLeave(object sender, MouseEventArgs e)
        {
            FindRectangle.Opacity = 1.0;
        }
    }
}