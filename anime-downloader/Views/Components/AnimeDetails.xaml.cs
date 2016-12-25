using System;
using System.Windows;
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

        private void Add(object sender, RoutedEventArgs routedEventArgs)
        {
            /*
            if (NameTextbox.Empty())
                Methods.Alert("There needs to be a name.");
            else
            {
                Anime.PreferredSubgroup = Anime.PreferredSubgroup.Equals("(None)") ? "" : Anime.PreferredSubgroup;
                Anime.Episode = Anime.Episode;
                // MainWindow.Window.AnimeCollection.Add(Anime);
                // TODO MainWindow.Window.AnimeList.Press();
            }
            */
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

        private void GoToNext()
        {
            /*
            var animes = MainWindow.Window.AnimeCollection.FilteredAndSorted.ToList();
            var anime = animes.First(an => an.Name.Equals(_anime.Name));
            var position = (animes.IndexOf(anime) + 1) % animes.Count;
            MainWindow.Window.DisplayTransition();
            MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(animes.ElementAt(position));
            */
        }

        private void GoToPrevious()
        {
            /*
            var animes = MainWindow.Window.AnimeCollection.FilteredAndSorted.ToList();
            var anime = animes.First(an => an.Name.Equals(_anime.Name));
            var position = animes.IndexOf(anime) - 1 >= 0 ? animes.IndexOf(anime) - 1 : animes.Count - 1;
            MainWindow.Window.DisplayTransition();
            MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(animes.ElementAt(position));
            */
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

        private void AnimeDetails_OnLoaded(object sender, RoutedEventArgs e)
        {
            PreviewKeyDown += AnimeDetails_OnKeyDown;
            Focusable = true;
            Focus();
        }
        
        // 

        private void LastEpisode_Click(object sender, RoutedEventArgs e) {} // Process.Start(Anime.LastEpisode.Path);
        
    }
}