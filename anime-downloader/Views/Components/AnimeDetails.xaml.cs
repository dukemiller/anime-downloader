using System;
using System.Windows;
using System.Windows.Input;

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
        
        // 

        private void LastEpisode_Click(object sender, RoutedEventArgs e) {} // Process.Start(Anime.LastEpisode.Path);


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