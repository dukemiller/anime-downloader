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
        public MyAnimeListBarViewModel(Anime anime, ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Anime = anime;
            Settings = settings;
            AnimeAggregate = animeAggregate;

            FindCommand = new RelayCommand(Find, () => Settings.MyAnimeListConfig.Works);
            ClearCommand = new RelayCommand(Clear);
            ProfileCommand = new RelayCommand(Profile);
            RefreshCommand = new RelayCommand(Refresh);

            Anime.MyAnimeList.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("Id"))
                    RaisePropertyChanged(nameof(HasId));
            };

            Settings.MyAnimeListConfig.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("Works"))
                    RaisePropertyChanged(nameof(LoggedIntoMal));
            };
        }

        private Anime Anime { get; }
        private ISettingsService Settings { get; }
        private IAnimeAggregateService AnimeAggregate { get; }

        // 

        public bool LoggedIntoMal => Settings.MyAnimeListConfig.Works;

        public Visibility HasId => Anime.MyAnimeList.HasId ? Visibility.Visible : Visibility.Collapsed;

        public RelayCommand FindCommand { get; set; }

        public RelayCommand ClearCommand { get; set; }

        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand RefreshCommand { get; set; }

        // 

        private async void Find()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});
            var id = await AnimeAggregate.MalService.GetId(Anime);
            RaisePropertyChanged(nameof(HasId));
            Settings.Save();
            if (!id)
                Methods.Alert($"No ID found for {Anime.Name}.");
            MessengerInstance.Send(new WorkMessage {Working = false});
        }

        private void Clear()
        {
            var response = MessageBox.Show("This will delete all saved MyAnimeList data about this show, are you sure?",
                "Confirmation",
                MessageBoxButton.YesNo);
            if (response == MessageBoxResult.Yes)
            {
                Anime.MyAnimeList = new MyAnimeListDetails {Id = null, NeedsUpdating = true};
                RaisePropertyChanged(nameof(HasId));
                Settings.Save();
            }
        }

        private void Profile() => Process.Start($"http://myanimelist.net/anime/{Anime.MyAnimeList.Id}");

        private async void Refresh()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});

            var animeResults = await AnimeAggregate.MalService.Find(HttpUtility.UrlEncode(Anime.MyAnimeList.Title));
            var result = animeResults.FirstOrDefault(r => r.Id.Equals(Anime.MyAnimeList.Id));

            if (result != null)
            {
                Anime.MyAnimeList.Synopsis = result.Synopsis;
                Anime.MyAnimeList.Image = result.Image;
                Anime.MyAnimeList.Title = result.Title;
                Anime.MyAnimeList.English = result.English;
                Anime.MyAnimeList.Synopsis = result.Synopsis;
                Anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;

                DateTime start;
                if (DateTime.TryParse(result.StartDate, out start))
                {
                    Anime.MyAnimeList.Aired = new AnimeSeason
                    {
                        Year = start.Year,
                        Season = (Season)Math.Ceiling(Convert.ToDouble(start.Month) / 3)
                    };
                }

                DateTime end;
                if (DateTime.TryParse(result.EndDate, out end))
                {
                    Anime.MyAnimeList.Ended = new AnimeSeason
                    {
                        Year = end.Year,
                        Season = (Season)Math.Ceiling(Convert.ToDouble(end.Month) / 3)
                    };

                    var now = DateTime.Now;
                    Anime.Airing = end.Year >= now.Year && (end.Month > now.Month ||
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