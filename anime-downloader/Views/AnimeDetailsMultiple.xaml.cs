using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using anime_downloader.Classes;
using anime_downloader.Enums;

namespace anime_downloader.Views
{
    public partial class AnimeDetailsMultiple
    {
        private IEnumerable<Anime> _animes;

        private ViewMode _viewMode;

        public AnimeDetailsMultiple()
        {
            InitializeComponent();
            KeyDown += AnimeList.KeyEscapeBack;
            MouseDown += AnimeList.MouseEscapeBack;
        }

        public void New()
        {
            _viewMode = ViewMode.Adding;
            EpisodeTextBox.Text = "00";
            RatingTextBox.Toggle();
        }

        public void Load(IList<Anime> animes)
        {
            _viewMode = ViewMode.Editing;

            // get the most used resolution, status, and airing from the selection,
            // then make them the value in the boxes
            _animes = animes;

            var resolution = animes.GroupBy(a => a.Resolution).OrderByDescending(c => c.Count()).First().Key;
            var status = animes.GroupBy(a => a.Status).OrderByDescending(c => c.Count()).First().Key;
            var airing = animes.GroupBy(a => a.Airing).OrderByDescending(c => c.Count()).First().Key;
            StatusComboBox.Text = status;
            AiringCheckBox.IsChecked = airing;
            ResolutionComboBox.Text = resolution;

            // Change the header and content
            InfoTextBlock.Text = "Make the same change to the following list of anime: ";
            InputTextBox.Text = string.Join("\n", animes.Select(a => a.Title));
            InputTextBox.IsReadOnly = true;
            
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

        private void SubmitButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Editing multiple
            if (_viewMode == ViewMode.Editing)
            {
                foreach (var anime in _animes)
                {
                    if (!RatingTextBox.Text.IsBlank())
                        anime.Rating = RatingTextBox.Text;
                    if (!EpisodeTextBox.Text.IsBlank())
                        anime.Episode = EpisodeTextBox.Text;
                    anime.Status = StatusComboBox.Text;
                    anime.Airing = AiringCheckBox.IsChecked == true;
                    anime.Resolution = ResolutionComboBox.Text;
                }
                MainWindow.Window.AnimeList.Press();
            }

            // Adding multiple
            else if (_viewMode == ViewMode.Adding)
            {
                var names = InputTextBox.Text
                    .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.ToLower())
                    .ToList();
                if (names.Distinct().Count() != names.Count)
                    Methods.Alert("Names have to be unique.");
                else if (MainWindow.Window.AllAnime.Select(a => a.Name.ToLower()).Intersect(names).Any())
                    Methods.Alert("A title entered already exists in the anime list.");
                else
                {
                    foreach (var name in names)
                    {
                        MainWindow.Window.AnimeCollection.Add(new Anime
                        {
                            Name = name,
                            Airing = AiringCheckBox.IsChecked ?? false,
                            Episode = $"{int.Parse(EpisodeTextBox.Text):D2}",
                            Status = StatusComboBox.Text,
                            Resolution = ResolutionComboBox.Text
                        });
                    }
                    MainWindow.Window.AnimeList.Press();
                }
            }
        }

        private void InputTextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_viewMode == ViewMode.Adding)
                InputTextBox.Focus();
        }
    }
}