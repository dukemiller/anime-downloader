using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Classes;

namespace anime_downloader.Views.Components
{
    public partial class AnimeDetails
    {
        public AnimeDetails()
        {
            InitializeComponent();
        }

        // 

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

        private void Rating_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textbox = (TextBox) sender;

            if (textbox.Text.Any(c => !char.IsDigit(c)) || e.Text.Any(c => !char.IsDigit(c)) ||
                e.Text.Length == 0 || e.Text.Trim().Equals(" ") || string.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }

            if (textbox.Text.Length == 0)
                return;

            var current = int.Parse(textbox.Text);
            var adder = int.Parse(e.Text);

            if (current == 10)
            {
                if (textbox.SelectionStart == 2)
                {
                    e.Handled = true;
                    textbox.Text = $"{adder}";
                    textbox.SelectionStart = 1;
                }

                else if (textbox.SelectedText.Length != textbox.Text.Length)
                {
                    e.Handled = true;
                }
            }

            else
            {
                if (adder == 0)
                {
                    if (current == 1)
                    {
                        textbox.Text = "10";
                        textbox.SelectionStart = 2;
                        e.Handled = true;
                        return;
                    }
                }
                e.Handled = true;
                textbox.Text = $"{adder}";
                textbox.SelectionStart = 1;
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