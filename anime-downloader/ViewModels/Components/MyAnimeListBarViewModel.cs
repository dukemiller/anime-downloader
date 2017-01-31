using System;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class MyAnimeListBarViewModel : ViewModelBase
    {
        private readonly Anime _anime;

        private readonly ISettingsService _settings;

        private readonly IAnimeAggregateService _animeAggregate;

        // 

        public MyAnimeListBarViewModel(Anime anime, ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            _anime = anime;
            _settings = settings;
            _animeAggregate = animeAggregate;

            FindCommand = new RelayCommand(Find, () => _settings.MyAnimeListConfig.Works);
            ClearCommand = new RelayCommand(Clear);
            ProfileCommand = new RelayCommand(Profile);
            RefreshCommand = new RelayCommand(Refresh);

            _anime.MyAnimeList.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("Id"))
                    RaisePropertyChanged(nameof(HasId));
            };

            _settings.MyAnimeListConfig.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("Works"))
                    RaisePropertyChanged(nameof(LoggedIntoMal));
            };
        }

        // 

        public bool LoggedIntoMal => _settings.MyAnimeListConfig.Works;

        public Visibility HasId => _anime.MyAnimeList.HasId ? Visibility.Visible : Visibility.Collapsed;

        public RelayCommand FindCommand { get; set; }

        public RelayCommand ClearCommand { get; set; }

        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand RefreshCommand { get; set; }

        // 

        private async void Find()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});
            var id = await _animeAggregate.MalService.GetId(_anime);
            RaisePropertyChanged(nameof(HasId));
            _settings.Save();
            if (!id)
                Methods.Alert($"No ID found for {_anime.Name}.");
            MessengerInstance.Send(new WorkMessage {Working = false});
        }

        private void Clear()
        {
            var response = MessageBox.Show("This will delete all saved MyAnimeList data about this show, are you sure?",
                "Confirmation",
                MessageBoxButton.YesNo);
            if (response == MessageBoxResult.Yes)
            {
                _anime.MyAnimeList = new MyAnimeListDetails {Id = null, NeedsUpdating = true};
                RaisePropertyChanged(nameof(HasId));
                _settings.Save();
            }
        }

        private void Profile() => Process.Start($"http://myanimelist.net/anime/{_anime.MyAnimeList.Id}");

        private async void Refresh()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});

            var animeResults = await _animeAggregate.MalService.Find(HttpUtility.UrlEncode(_anime.MyAnimeList.Title));
            var result = animeResults.FirstOrDefault(r => r.Id.Equals(_anime.MyAnimeList.Id));

            if (result != null)
            {
                _anime.MyAnimeList.Synopsis = result.Synopsis;
                _anime.MyAnimeList.Image = result.Image;
                _anime.MyAnimeList.Title = result.Title;
                _anime.MyAnimeList.English = result.English;
                _anime.MyAnimeList.Synopsis = result.Synopsis;
                _anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;

                DateTime start;
                if (DateTime.TryParse(result.StartDate, out start))
                {
                    _anime.MyAnimeList.Aired = new AnimeSeason
                    {
                        Year = start.Year,
                        Season = (Season)Math.Ceiling(Convert.ToDouble(start.Month) / 3)
                    };
                }

                DateTime end;
                if (DateTime.TryParse(result.EndDate, out end))
                {
                    _anime.MyAnimeList.Ended = new AnimeSeason
                    {
                        Year = end.Year,
                        Season = (Season)Math.Ceiling(Convert.ToDouble(end.Month) / 3)
                    };

                    var now = DateTime.Now;
                    _anime.Airing = end.Year >= now.Year && (end.Month > now.Month ||
                                                            end.Month == now.Month && end.Day > now.Day);
                }
            }

            else
            {
                Methods.Alert("Had trouble finding this show on MAL.");
            }

            RaisePropertyChanged(nameof(HasId));
            MessengerInstance.Send(new WorkMessage {Working = false});
        }
    }
}