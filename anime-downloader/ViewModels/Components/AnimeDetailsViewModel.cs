using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class AnimeDetailsViewModel : ViewModelBase
    {
        private Anime _anime;
        private RelayCommand _buttonCommand;
        private string _buttonText;
        private bool _lastEpisodeAvailable;
        private MyAnimeListBarViewModel _myAnimeListBar;
        private string _selectedSubgroup;
        private ISettingsService _settings;

        // 

        /// <summary>
        ///     Edit
        /// </summary>
        public AnimeDetailsViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate, Anime anime)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;
            Anime = anime;
            ButtonText = "Edit";

            // 

            ButtonCommand = new RelayCommand(
                Edit,
                () =>
                    !AnimeAggregate.AnimeService.Animes.Except(new[] {Anime})
                        .Any(a => a.Name.ToLower().Trim().Equals(Anime?.Name?.ToLower().Trim()))
                    && Anime?.Name?.Length > 0
            );

            ExitCommand = new RelayCommand(() =>
            {
                Settings.Save();
                MessengerInstance.Send(Enums.Views.AnimeDisplay);
            });
            MyAnimeListBar = new MyAnimeListBarViewModel(Anime, Settings, AnimeAggregate);
            NextCommand = new RelayCommand(Next);
            PreviousCommand = new RelayCommand(Previous);

            // Default of true to avoid the flicker on the majority case that the file is found
            LastEpisodeAvailable = true;
            LastEpisodeCommand = new RelayCommand(
                PlayLastEpisode,
                () => LastEpisodeAvailable
            );
            // It's an expensive operation so it has to be async or creating the view makes an upward
            // of 400ms delay
            GetLastEpisode();
        }

        /// <summary>
        ///     Create
        /// </summary>
        public AnimeDetailsViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate)
        {
            Settings = settings;
            AnimeAggregate = animeAggregate;
            Anime = new Anime
            {
                Episode = 0,
                Status = Status.Watching,
                Resolution = "720",
                Airing = true
            };
            ButtonText = "Add";
            ButtonCommand = new RelayCommand(
                Create,
                () =>
                    !AnimeAggregate.AnimeService.Animes.Any(
                        a => a.Name.ToLower().Trim().Equals(Anime?.Name?.ToLower().Trim()))
                    && Anime?.Name?.Length > 0
            );
            ExitCommand = new RelayCommand(() => MessengerInstance.Send(Enums.Views.AnimeDisplay));
        }

        // 

        public static IEnumerable<Status> Statuses => Enum.GetValues(typeof(Status)).Cast<Status>();

        public ISettingsService Settings
        {
            get { return _settings; }
            set { Set(() => Settings, ref _settings, value); }
        }

        private IAnimeAggregateService AnimeAggregate { get; }

        private bool LastEpisodeAvailable
        {
            get { return _lastEpisodeAvailable; }
            set { Set(() => LastEpisodeAvailable, ref _lastEpisodeAvailable, value); }
        }

        public string ButtonText
        {
            get { return _buttonText; }
            set { Set(() => ButtonText, ref _buttonText, value); }
        }

        public RelayCommand ButtonCommand
        {
            get { return _buttonCommand; }
            set { Set(() => ButtonCommand, ref _buttonCommand, value); }
        }

        public RelayCommand LastEpisodeCommand { get; set; }

        public RelayCommand ExitCommand { get; set; }

        public RelayCommand NextCommand { get; set; }

        public RelayCommand PreviousCommand { get; set; }

        public MyAnimeListBarViewModel MyAnimeListBar
        {
            get { return _myAnimeListBar; }
            set { Set(() => MyAnimeListBar, ref _myAnimeListBar, value); }
        }

        public string SelectedSubgroup
        {
            get { return _selectedSubgroup; }
            set { Set(() => SelectedSubgroup, ref _selectedSubgroup, value); }
        }

        public Anime Anime
        {
            get { return _anime; }
            set
            {
                Set(() => Anime, ref _anime, value);
                Anime.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName.Equals("Name"))
                        ButtonCommand.RaiseCanExecuteChanged();
                };
            }
        }

        // 

        private async void GetLastEpisode()
        {
            await Task.Run(() =>
            {
                var path = AnimeAggregate.FileService.LastEpisode(Anime);
                LastEpisodeAvailable = path != null;
                Application.Current.Dispatcher.InvokeAsync(() => LastEpisodeCommand.RaiseCanExecuteChanged());
            });
        }

        private void PlayLastEpisode() => Process.Start(AnimeAggregate.FileService.LastEpisode(Anime).Path);

        private void Edit()
        {
            Settings.Save();
            MessengerInstance.Send(Enums.Views.AnimeDisplay);
        }

        private void Create()
        {
            if (!string.IsNullOrEmpty(SelectedSubgroup))
                Anime.PreferredSubgroup = SelectedSubgroup;
            Settings.Animes.Add(Anime);
            MessengerInstance.Send(Enums.Views.AnimeDisplay);
        }

        private void Next()
        {
            Settings.Save();
            var animes = AnimeAggregate.AnimeService.FilteredAndSorted().ToList();
            var anime = animes.First(an => an.Name.Equals(Anime.Name));
            var position = (animes.IndexOf(anime) + 1) % animes.Count;
            MessengerInstance.Send(animes.ElementAt(position));
        }

        private void Previous()
        {
            Settings.Save();
            var animes = AnimeAggregate.AnimeService.FilteredAndSorted().ToList();
            var anime = animes.First(an => an.Name.Equals(Anime.Name));
            var position = animes.IndexOf(anime) - 1 >= 0 ? animes.IndexOf(anime) - 1 : animes.Count - 1;
            MessengerInstance.Send(animes.ElementAt(position));
        }
    }
}