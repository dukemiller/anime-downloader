using anime_downloader.Classes;
using System;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Classes.Web.MyAnimeList;

namespace anime_downloader.Views
{
    public partial class AnimeDetails
    {
        private Anime _currentlyEditedAnime;

        public AnimeDetails()
        {
            InitializeComponent();
            KeyDown += AnimeList.KeyEscapeBack;
            MouseDown += AnimeList.MouseEscapeBack;
            MainWindow.Window.Settings.Subgroups.ToList().ForEach(s => SubgroupComboBox.Items.Add(s));
        }

        public void Load(Anime anime)
        {
            DataContext = anime;
            SubmitButton.Content = "Edit";
            SubmitButton.Click += Edit;
            _currentlyEditedAnime = anime;
            SubgroupComboBox.Text = anime.PreferredSubgroup != null && anime.PreferredSubgroup.Equals("")
                ? "(None)"
                : anime.PreferredSubgroup;
        }

        public void New()
        {
            // Default template
            DataContext = new Anime
            {
                Episode = "00",
                Status = "Watching",
                Resolution = "720",
                Airing = true
            };

            SubmitButton.Click += Add;
            OpenLastButton.Visibility = Visibility.Hidden;
        }

        private void Add(object sender, RoutedEventArgs routedEventArgs)
        {
            if (NameTextbox.Empty())
                Methods.Alert("There needs to be a name.");
            else
            {
                var subgroup = SubgroupComboBox.Text;
                if (subgroup.Equals("(None)"))
                    subgroup = "";

                var episode = EpisodeTextbox.Text.Length > 0
                    ? $"{int.Parse(EpisodeTextbox.Text):D2}"
                    : "00";

                var status = StatusContainerGrid.GetAll<RadioButton>()
                    .First(radio => radio.IsChecked != null && radio.IsChecked.Value)
                    .Content.ToString();

                var resolution = ResolutionContainerGrid.GetAll<RadioButton>()
                    .First(radio => radio.IsChecked != null && radio.IsChecked.Value)
                    .Content.ToString();

                MainWindow.Window.AnimeCollection.Add(new Anime
                {
                    Name = NameTextbox.Text,
                    Episode = episode,
                    Status = status,
                    Resolution = resolution,
                    Airing = AiringCheckbox.IsChecked ?? false,
                    NameStrict = NameStrictCheckbox.IsChecked ?? false,
                    PreferredSubgroup = subgroup,
                    Rating = RatingTextbox.Text
                });

                MainWindow.Window.AnimeList.Press();
            }
        }

        private void Edit(object sender, RoutedEventArgs routedEventArgs)
        {
            if (NameTextbox.Empty())
                Methods.Alert("There needs to be a name.");
            else
            {
                var subgroup = SubgroupComboBox.Text;
                _currentlyEditedAnime.PreferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup;
                if (_currentlyEditedAnime.Status.Equals("Finished") && _currentlyEditedAnime.Airing)
                    _currentlyEditedAnime.Airing = false;
                MainWindow.Window.Cycle(MainWindow.Window.AnimeList);
            }
        }

        // 

        private void GoToNext()
        {
            var animes = MainWindow.Window.AnimeCollection.FilteredAndSorted().ToList();
            var anime = animes.First(an => an.Name.Equals(_currentlyEditedAnime.Name));
            var position = (animes.IndexOf(anime) + 1) % animes.Count;
            MainWindow.Window.DisplayTransition();
            MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(animes.ElementAt(position));

        }

        private void GoToPrevious()
        {
            var animes = MainWindow.Window.AnimeCollection.FilteredAndSorted().ToList();
            var anime = animes.First(an => an.Name.Equals(_currentlyEditedAnime.Name));
            var position = animes.IndexOf(anime) - 1 >= 0 ? animes.IndexOf(anime) - 1 : animes.Count - 1;
            MainWindow.Window.DisplayTransition();
            MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(animes.ElementAt(position));
        }

        // 

        private void EpisodeTextbox_GotFocus(object sender, RoutedEventArgs e) => EpisodeTextbox.SelectAll();

        private void NameTextbox_GotFocus(object sender, RoutedEventArgs e) => NameTextbox.SelectAll();

        private void EpisodeTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        private void RatingTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => Methods.AnimeRatingRules(RatingTextbox, e);


        private void EnterApply(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            SubmitButton.Focus();
            SubmitButton.Press();
        }

        private void NameTextbox_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (SubmitButton.Content.Equals("Add"))
                NameTextbox.Focus();
        }

        private void GotoMalButton_OnClick(object sender, RoutedEventArgs e) => Process.Start($"http://myanimelist.net/anime/{_currentlyEditedAnime.MyAnimeList.Id}");

        private void ClearMalButton_OnClick(object sender, RoutedEventArgs e)
        {
            _currentlyEditedAnime.MyAnimeList.Id = "";
            _currentlyEditedAnime.MyAnimeList.NeedsUpdating = true;
            _currentlyEditedAnime.MyAnimeList.SeriesContinuationEpisode = "";
            _currentlyEditedAnime.MyAnimeList.TotalEpisodes = "";
            _currentlyEditedAnime.MyAnimeList.English = "";
            _currentlyEditedAnime.MyAnimeList.Image = "";
            _currentlyEditedAnime.MyAnimeList.Synopsis = "";
            _currentlyEditedAnime.MyAnimeList.Title = "";
            _currentlyEditedAnime.MyAnimeList.Synonyms = "";
            Methods.Alert("Cleared all MyAnimeList data about this show.");
            MalDockPanel.Visibility = Visibility.Hidden;
        }

        private async void RefreshMalButton_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Window.ToggleButtons();
            var credentials = Api.GetCredentials(MainWindow.Window.Settings);
            var animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(_currentlyEditedAnime.Title));
            var result = animeResults.FirstOrDefault(r => r.Id.Equals(_currentlyEditedAnime.MyAnimeList.Id));

            if (result != null)
            {
                _currentlyEditedAnime.MyAnimeList.Synopsis = result.Synopsis;
                _currentlyEditedAnime.MyAnimeList.Image = result.Image;
                _currentlyEditedAnime.MyAnimeList.Title = result.Title;
                _currentlyEditedAnime.MyAnimeList.English = result.English;
                _currentlyEditedAnime.MyAnimeList.Synopsis = result.Synopsis;
                _currentlyEditedAnime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;
                Methods.Alert("Updated any information about this show");
            }

            MainWindow.Window.ToggleButtons();
        }

        private void OpenLastButton_OnClick(object sender, RoutedEventArgs e)
        {
            var episode = MainWindow.Window.AnimeFileCollection.LastEpisodeOf(_currentlyEditedAnime);
            if (episode == null)
                Methods.Alert($"Episode {_currentlyEditedAnime.Episode} for '{_currentlyEditedAnime.Name}' not found in any directory.");
            else
                Process.Start($"{episode.Path}");
        }

        private void AnimeDetails_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
                GoToNext();
            else if (e.Key == Key.Left)
                GoToPrevious();
        }

        private void AnimeDetails_OnLoaded(object sender, RoutedEventArgs e)
        {
            PreviewKeyDown += AnimeDetails_OnKeyDown;
            Focusable = true;
            Focus();
        }
    }
}