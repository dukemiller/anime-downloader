using System;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace anime_downloader.Views.Components.AnimeDisplay
{
    /// <summary>
    /// Interaction logic for Details.xaml
    /// </summary>
    public partial class Details
    {
        public Details()
        {
            InitializeComponent();
        }

        private void Number_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (!char.IsDigit(e.Text, e.Text.Length - 1))
                    e.Handled = true;
            }

            catch (Exception)
            {
                // pass
            }
        }

        private static readonly Regex NumberPattern = new Regex(@"^(?:10|[1-9])$");

        private void Rating_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textbox = (TextBox) sender;
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
            var textbox = (TextBox) sender;
            if (!NumberPattern.IsMatch(textbox.Text))
            {
                textbox.Text = "";
                textbox.SelectionStart = 0;
            }
        }

        // 

        /// <summary>
        ///     This is necessary to defocus from currently selected textboxes on other elements that aren't
        ///     inputs e.g. the grid, to allow input bindings set on the user control
        /// </summary>
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            Focus();
        }
    }
}
