using anime_downloader.Classes;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Annotations;
using anime_downloader.Classes.Web.MyAnimeList;

namespace anime_downloader.Views
{
    public partial class AnimeDetails : INotifyPropertyChanged
    {
        private Anime _anime;

        public Anime Anime
        {
            get { return _anime; }
            set
            {
                _anime = value;
                OnPropertyChanged();
            }
        }

        private string _buttonText;

        public string ButtonText
        {
            get { return _buttonText; }
            set
            {
                _buttonText = value;
                OnPropertyChanged();
            }
        }

        public AnimeDetails()
        {
            InitializeComponent();
            KeyDown += AnimeList.KeyEscapeBack;
            MouseDown += AnimeList.MouseEscapeBack;
            MainWindow.Window.Settings.Subgroups.ToList().ForEach(s => SubgroupComboBox.Items.Add(s));
        }

        public void Load(Anime anime)
        {
            Anime = anime;
            ButtonText = "Edit";
            SubmitButton.Click += Edit;

            SubgroupComboBox.Text = anime.PreferredSubgroup != null && anime.PreferredSubgroup.Equals("")
                ? "(None)"
                : anime.PreferredSubgroup;
        }

        public void New()
        {
            // Default template
            Anime = new Anime
            {
                Episode = "00",
                Status = "Considering",
                Resolution = "720",
                Airing = true
            };
            ButtonText = "Add";
            SubmitButton.Click += Add;
        }

        private void Add(object sender, RoutedEventArgs routedEventArgs)
        {
            if (NameTextbox.Empty())
                Methods.Alert("There needs to be a name.");
            else
            {
                Anime.PreferredSubgroup = Anime.PreferredSubgroup.Equals("(None)") ? "" : Anime.PreferredSubgroup;
                Anime.Episode = Anime.Episode.Length > 0 ? $"{int.Parse(Anime.Episode):D2}" : "00";
                MainWindow.Window.AnimeCollection.Add(Anime);
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
                _anime.PreferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup;
                if (_anime.Status.Equals("Finished") && _anime.Airing)
                    _anime.Airing = false;
                MainWindow.Window.Cycle(MainWindow.Window.AnimeList);
            }
        }

        // 

        private void GoToNext()
        {
            var animes = MainWindow.Window.AnimeCollection.FilteredAndSorted.ToList();
            var anime = animes.First(an => an.Name.Equals(_anime.Name));
            var position = (animes.IndexOf(anime) + 1) % animes.Count;
            MainWindow.Window.DisplayTransition();
            MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(animes.ElementAt(position));
        }

        private void GoToPrevious()
        {
            var animes = MainWindow.Window.AnimeCollection.FilteredAndSorted.ToList();
            var anime = animes.First(an => an.Name.Equals(_anime.Name));
            var position = animes.IndexOf(anime) - 1 >= 0 ? animes.IndexOf(anime) - 1 : animes.Count - 1;
            MainWindow.Window.DisplayTransition();
            MainWindow.Window.ChangeDisplay<AnimeDetails>().Load(animes.ElementAt(position));
        }

        // 

        private void EpisodeTextbox_GotFocus(object sender, RoutedEventArgs e) => (sender as TextBox)?.SelectAll();

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
            => Methods.AnimeRatingRules(sender as TextBox, e);

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

        private void AnimeDetails_OnKeyDown(object sender, KeyEventArgs e)
        {
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
        }

        private void AnimeDetails_OnLoaded(object sender, RoutedEventArgs e)
        {
            PreviewKeyDown += AnimeDetails_OnKeyDown;
            Focusable = true;
            Focus();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            Focus();
        }

        // 

        private void LastEpisode_Click(object sender, RoutedEventArgs e) => Process.Start(Anime.LastEpisode.Path);

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Window.ToggleButtons();
            var credentials = Api.GetCredentials(MainWindow.Window.Settings);
            var animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(_anime.MyAnimeList.Title));
            var result = animeResults.FirstOrDefault(r => r.Id.Equals(_anime.MyAnimeList.Id));

            if (result != null)
            {
                _anime.MyAnimeList.Synopsis = result.Synopsis;
                _anime.MyAnimeList.Image = result.Image;
                _anime.MyAnimeList.Title = result.Title;
                _anime.MyAnimeList.English = result.English;
                _anime.MyAnimeList.Synopsis = result.Synopsis;
                _anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;
                Methods.Alert("Updated any information about this show");
            }

            else
            {
                Methods.Alert("Had trouble finding this show on MAL.");
            }

            MainWindow.Window.ToggleButtons();
        }

        private void Profile_Click(object sender, RoutedEventArgs e) => Process.Start($"http://myanimelist.net/anime/{_anime.MyAnimeList.Id}");

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var response =
                MessageBox.Show(
                    "This will delete all MyAnimeList data about this show received from attempting to synchronize. " +
                    "Are you sure?",
                    "Confirmation",
                    MessageBoxButton.YesNo);

            if (response == MessageBoxResult.Yes)
            {
                _anime.MyAnimeList.Id = "";
                _anime.MyAnimeList.NeedsUpdating = true;
                _anime.MyAnimeList.SeriesContinuationEpisode = "";
                _anime.MyAnimeList.TotalEpisodes = "";
                _anime.MyAnimeList.English = "";
                _anime.MyAnimeList.Image = "";
                _anime.MyAnimeList.Synopsis = "";
                _anime.MyAnimeList.Title = "";
                _anime.MyAnimeList.Synonyms = "";
                Methods.Alert("Cleared all MyAnimeList data about this show.");
            }
        }

        private async void Find_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Window.ToggleButtons();
            var credentials = Api.GetCredentials(MainWindow.Window.Settings);
            var id = await Synchronizer.GetId(_anime, credentials);
            var words = id ? "found" : "not found";
            Methods.Alert($"MAL ID {words} for {_anime.Name}.");
            MainWindow.Window.ToggleButtons();
        }

        // 

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}