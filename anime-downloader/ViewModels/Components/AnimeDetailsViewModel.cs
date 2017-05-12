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
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.ViewModels.Components
{
    public class AnimeDetailsViewModel : ViewModelBase
    {
        private readonly IAnimeService _animeService;
        private Anime _anime;
        private RelayCommand _buttonCommand;
        private string _buttonText;
        private MyAnimeListBarViewModel _myAnimeListBar;
        private string _selectedSubgroup;
        private ISettingsService _settings;

        // 

        public AnimeDetailsViewModel(ISettingsService settings, IAnimeService animeService)
        {
            Settings = settings;
            _animeService = animeService;
        }

        // 

        public AnimeDetailsViewModel EditExisting(Anime anime)
        {
            Anime = anime;
            SelectedSubgroup = Anime.PreferredSubgroup;

            ButtonText = "Edit";

            ButtonCommand = new RelayCommand(
                Edit,
                () => Anime?.Name?.Length > 0
            );

            ExitCommand = new RelayCommand(() =>
            {
                Settings.Save();
                MessengerInstance.Send(Enums.ViewDisplay.Anime);
            });

            MyAnimeListBar = SimpleIoc.Default.GetInstance<MyAnimeListBarViewModel>().Load(Anime);

            NextCommand = new RelayCommand(Next);

            PreviousCommand = new RelayCommand(Previous);

            ClearSubgroupCommand = new RelayCommand(() => SelectedSubgroup = null);

            return this;
        }

        public AnimeDetailsViewModel CreateNew()
        {
            Anime = new Anime
            {
                Episode = 0,
                Status = Status.Watching,
                Resolution = "720",
                Airing = true,
                MyAnimeList = { NeedsUpdating = true }
            };

            ButtonText = "Add";

            SelectedSubgroup = Anime.PreferredSubgroup;

            ButtonCommand = new RelayCommand(
                Create,
                () =>
                    !_animeService.Animes.Any(
                        a => a.Name.ToLower().Trim().Equals(Anime?.Name?.ToLower().Trim()))
                    && Anime?.Name?.Length > 0
            );

            ExitCommand = new RelayCommand(() => MessengerInstance.Send(Enums.ViewDisplay.Anime));

            ClearSubgroupCommand = new RelayCommand(() => SelectedSubgroup = null);

            return this;
        }

        // 

        public static IEnumerable<Status> Statuses => Enum.GetValues(typeof(Status)).Cast<Status>();

        public ISettingsService Settings
        {
            get => _settings;
            set => Set(() => Settings, ref _settings, value);
        }

        public string ButtonText
        {
            get => _buttonText;
            set => Set(() => ButtonText, ref _buttonText, value);
        }

        public RelayCommand ButtonCommand
        {
            get => _buttonCommand;
            set => Set(() => ButtonCommand, ref _buttonCommand, value);
        }

        public RelayCommand ExitCommand { get; set; }

        public RelayCommand NextCommand { get; set; }

        public RelayCommand PreviousCommand { get; set; }

        public RelayCommand ClearSubgroupCommand { get; set; }

        public MyAnimeListBarViewModel MyAnimeListBar
        {
            get => _myAnimeListBar;
            set => Set(() => MyAnimeListBar, ref _myAnimeListBar, value);
        }

        public string SelectedSubgroup
        {
            get => _selectedSubgroup;
            set
            {
                Set(() => SelectedSubgroup, ref _selectedSubgroup, value);
                Anime.PreferredSubgroup = SelectedSubgroup;
            }
        }

        public Anime Anime
        {
            get => _anime;
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

        private void Edit()
        {
            Settings.Save();
            MessengerInstance.Send(Enums.ViewDisplay.Anime);
        }

        private void Create()
        {
            if (!string.IsNullOrEmpty(SelectedSubgroup))
                Anime.PreferredSubgroup = SelectedSubgroup;
            _animeService.Add(Anime);
            MessengerInstance.Send(Enums.ViewDisplay.Anime);
        }

        private void Next()
        {
            Settings.Save();
            var animes = _animeService.FilteredAndSorted().ToList();
            var anime = animes.First(an => an.Name.Equals(Anime.Name));
            var position = (animes.IndexOf(anime) + 1) % animes.Count;
            MessengerInstance.Send(animes.ElementAt(position));
        }

        private void Previous()
        {
            Settings.Save();
            var animes = _animeService.FilteredAndSorted().ToList();
            var anime = animes.First(an => an.Name.Equals(Anime.Name));
            var position = animes.IndexOf(anime) - 1 >= 0 ? animes.IndexOf(anime) - 1 : animes.Count - 1;
            MessengerInstance.Send(animes.ElementAt(position));
        }
    }
}