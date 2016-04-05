using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for AnimeDetails.xaml
    /// </summary>
    public partial class AnimeDetails
    {
        public AnimeDetails()
        {
            InitializeComponent();
        }

        private void EpisodeTextbox_GotFocus(object sender, RoutedEventArgs e)
        {
            EpisodeTextbox.SelectAll();
        }

        private void NameTextbox_GotFocus(object sender, RoutedEventArgs e)
        {
            NameTextbox.SelectAll();
        }

        private void EpisodeTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void RatingTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int total;
            int toAdd;
            
            // Only numbers allowed
            if (RatingTextbox.Text.Any(c => !char.IsDigit(c)) || e.Text.Any(c => !char.IsDigit(c)))
            {
                e.Handled = true;
            }

            if (!RatingTextbox.SelectionLength.Equals(2) && int.TryParse(RatingTextbox.Text, out total) && int.TryParse(e.Text, out toAdd))
            {
                toAdd *= (int) Math.Pow(10, RatingTextbox.Text.Length + 1);
                if (total + toAdd > 10 || toAdd == 0)
                {
                    RatingTextbox.Text = "10";
                    e.Handled = true;
                    RatingTextbox.Select(0, 2);
                }
            }
        }
    }
}