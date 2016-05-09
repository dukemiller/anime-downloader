using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for AnimeDetailsMultiple.xaml
    /// </summary>
    public partial class AnimeDetailsMultiple
    {
        public AnimeDetailsMultiple()
        {
            InitializeComponent();
        }

        private void EpisodeTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            EpisodeTextBox.SelectAll();
        }

        private void EpisodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void RatingTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int total;
            int toAdd;

            // Only numbers allowed
            if (RatingTextBox.Text.Any(c => !char.IsDigit(c)) || e.Text.Any(c => !char.IsDigit(c)))
            {
                e.Handled = true;
            }

            if (!RatingTextBox.SelectionLength.Equals(2) &&
                int.TryParse(RatingTextBox.Text, out total) && int.TryParse(e.Text, out toAdd))
            {
                toAdd *= (int) Math.Pow(10, RatingTextBox.Text.Length + 1);
                if (total + toAdd > 10 || toAdd == 0)
                {
                    RatingTextBox.Text = "10";
                    e.Handled = true;
                    RatingTextBox.Select(0, 2);
                }
            }
        }
    }
}