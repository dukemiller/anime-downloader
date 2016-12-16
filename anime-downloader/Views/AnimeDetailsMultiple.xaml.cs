using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Annotations;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;

namespace anime_downloader.Views
{
    public partial class AnimeDetailsMultiple : INotifyPropertyChanged
    {
        private IEnumerable<Anime> _animes;
        
        private ViewMode _currentViewMode;

        public ViewMode CurrentViewMode
        {
            get { return _currentViewMode; }
            set
            {
                _currentViewMode = value;
                OnPropertyChanged();
            }
        }

        private string _input;

        public string Input
        {
            get { return _input; }
            set
            {
                if (value == _input) return;
                _input = value;
                OnPropertyChanged();
            }
        }

        private string _header;

        public string Header
        {
            get { return _header; }
            set
            {
                _header = value;
                OnPropertyChanged();
            }
        }

        public Models.AnimeDetails Details { get; set; }

        public AnimeDetailsMultiple()
        {
            Details = new Models.AnimeDetails();
            InitializeComponent();
            KeyDown += AnimeList.KeyEscapeBack;
            MouseDown += AnimeList.MouseEscapeBack;
        }

        public void New()
        {
            CurrentViewMode = ViewMode.Adding;
            Header = "Put each anime on own line, each will be added with the template chosen below: ";
            Details.Resolution = "720";
            Details.Episode = "00";
            Details.Airing = true;
            Details.Status = "Considering";
        }

        public void Load(IList<Anime> animes)
        {
            CurrentViewMode = ViewMode.Editing;
            Header = "Make the same change to the following list of anime: ";
            _animes = animes;

            // get the most used resolution, status, and airing from the selection,
            // then make them the value in the boxes
            Input = string.Join("\n", animes.Select(a => a.Title));
            Details.Resolution = animes.GroupBy(a => a.Resolution).OrderByDescending(c => c.Count()).First().Key;
            Details.Status = animes.GroupBy(a => a.Status).OrderByDescending(c => c.Count()).First().Key;
            Details.Airing = animes.GroupBy(a => a.Airing).OrderByDescending(c => c.Count()).First().Key;
            
        }

        private void EpisodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void RatingTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => Methods.AnimeRatingRules(RatingTextBox, e);

        private void SubmitButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Editing multiple
            if (_currentViewMode == ViewMode.Editing)
            {
                foreach (var anime in _animes)
                {
                    if (!Details.Rating.IsBlank())
                        anime.Rating = Details.Rating;
                    if (!Details.Episode.IsBlank())
                        anime.Episode = Details.Episode;
                    anime.Status = Details.Status;
                    anime.Airing = Details.Airing;
                    anime.Resolution = Details.Resolution;
                }

                MainWindow.Window.AnimeList.Press();
            }

            // Adding multiple
            else if (_currentViewMode == ViewMode.Adding)
            {
                var names = Input
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
                            Airing = Details.Airing,
                            Episode = $"{int.Parse(Details.Episode):D2}",
                            Status = Details.Status,
                            Resolution = Details.Resolution
                        });
                    }
                    MainWindow.Window.AnimeList.Press();
                }
            }
        }

        private void InputTextBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_currentViewMode == ViewMode.Adding)
                (sender as TextBox)?.Focus();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}