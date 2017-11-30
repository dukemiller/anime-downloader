using System.Text.RegularExpressions;
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
        
        private static readonly Regex NumberPattern = new Regex(@"^(?:10|[1-9])$");

        private void Rating_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textbox = (TextBox)sender;
            var currentRating = textbox.Text;

            e.Handled = true;

            if (currentRating.Length == 1)
            {
                if (NumberPattern.IsMatch(currentRating + e.Text))
                {
                    textbox.Text = currentRating + e.Text;
                    textbox.SelectionStart = 2;
                }

                else if (NumberPattern.IsMatch(e.Text))
                {
                    textbox.Text = e.Text;
                    textbox.SelectionStart = 1;
                }

            }

            else
            {
                if (NumberPattern.IsMatch(e.Text))
                {
                    textbox.Text = e.Text;
                    textbox.SelectionStart = 1;
                }
            }
        }

        private void Rating_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)sender;
            if (!NumberPattern.IsMatch(textbox.Text))
            {
                textbox.Text = "";
                textbox.SelectionStart = 0;
            }
        }
    }
}