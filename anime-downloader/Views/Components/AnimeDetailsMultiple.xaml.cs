using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Classes;

namespace anime_downloader.Views.Components
{
    public partial class AnimeDetailsMultiple
    {
        public AnimeDetailsMultiple()
        {
            InitializeComponent();
        }

        private void Episode_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void Rating_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => Methods.AnimeRatingRules(sender as TextBox, e);
    }
}