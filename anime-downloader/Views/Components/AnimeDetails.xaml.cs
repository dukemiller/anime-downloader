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
        
        private void Edit(object sender, RoutedEventArgs routedEventArgs)
        {
            /*
            if (NameTextbox.Empty())
                Methods.Alert("There needs to be a name.");
            else
            {
                var subgroup = SubgroupComboBox.Text;
                _anime.PreferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup;
                if (_anime.Status.Equals("Finished") && _anime.Airing)
                    _anime.Airing = false;
                // TODO MainWindow.Window.Cycle(MainWindow.Window.AnimeList);
            }
            */
        }

        // 

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
        
        private void AnimeDetails_OnKeyDown(object sender, KeyEventArgs e)
        {
            /*
            // So you can type without changing the view
            if (Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is PasswordBox)
                return;

            if (ButtonText.Equals("Edit"))
            {
                if (e.Key == Key.Right)
                {
                    GoToNext();
                    e.Handled = true;
                }
                else if (e.Key == Key.Left)
                {
                    GoToPrevious();
                    e.Handled = true;
                }
            }
            */
        }
        
        // 

        private void LastEpisode_Click(object sender, RoutedEventArgs e) {} // Process.Start(Anime.LastEpisode.Path);
        
    }
}