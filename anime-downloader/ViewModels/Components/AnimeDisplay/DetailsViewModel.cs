using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Optional;
using Optional.Collections;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.ViewModels.Components.AnimeDisplay
{
    public class DetailsViewModel : ViewModelBase
    {
        private static IFindSeasonAnimeService FindSeasonAnimeService => SimpleIoc.Default.GetInstance<IFindSeasonAnimeService>();

        private static readonly WebClient Downloader = new WebClient();

        private readonly IAnimeService _animeService;

        private bool _changeMade;

        // 

        public DetailsViewModel(IAnimeRepository animeRepository, ISettingsRepository settingsRepository, IAnimeService animeService)
        {
            AnimeRepository = animeRepository;
            SettingsRepository = settingsRepository;
            _animeService = animeService;
        }

        // 

        public DetailsViewModel EditExisting(Anime anime)
        {
            Editing = true;
            Anime = anime;
            SetupImage();

            ExitCommand = new RelayCommand(() =>
            {
                AnimeRepository.Save();
                MessengerInstance.Send(Display.Anime);
            });

            DetailsBar = SimpleIoc.Default.GetInstance<DetailsBarViewModel>().Load(Anime);
            NextCommand = new RelayCommand(Next);
            PreviousCommand = new RelayCommand(Previous);

            // Button
            Text = "Edit";
            Command = new RelayCommand(
                Edit,
                () => Anime?.Name?.Length > 0
            );

            Anime.PropertyChanged -= AnimeOnPropertyChanged;
            Anime.PropertyChanged += AnimeOnPropertyChanged;

            SetAirDay();

            return this;
        }

        public DetailsViewModel CreateNew()
        {
            Editing = false;
            Anime = new Anime
            {
                Episode = 0,
                Status = Status.Watching,
                Resolution = "720",
                Airing = true,
                Details = { NeedsUpdating = true, JustAdded = true }
            };

            Image = "../../../Resources/Images/default.png";

            ExitCommand = new RelayCommand(() => MessengerInstance.Send(Display.Anime));

            // Button
            Text = "Add";
            Command = new RelayCommand(
                Create,
                () =>
                    !_animeService.Animes.Any(anime => Anime.Name.ToLower().Trim().Equals(anime.Name.ToLower().Trim()))
                    && Anime?.Name?.Length > 0
            );

            Anime.PropertyChanged -= AnimeOnPropertyChanged;
            Anime.PropertyChanged += AnimeOnPropertyChanged;

            return this;
        }

        public DetailsViewModel CreateNewFromAiring(AiringAnime airing)
        {
            Editing = false;
            Anime = new Anime
            {
                Name = airing.Title.Main,
                Episode = 0,
                Status = Status.Watching,
                Resolution = "720",
                Airing = true,
                Details =
                {
                    AniId = airing.Id,
                    Id = airing.IdMal?.ToString(),
                    NeedsUpdating = true,
                    JustAdded = true,
                    Image = airing.CoverImage.Large,
                    Synopsis = airing.Description,
                    Title = airing.Title.Romaji,
                    English = airing.Title.English,
                    Aired = AnimeSeason.Current,
                    TotalEpisodes = airing.Episodes ?? 0,
                    OverallTotal = airing.Episodes ?? 0
                }
            };

            Image = airing.CoverImage.Large;

            // 
            Text = "Add";
            Command = new RelayCommand(
                CreateAndReturn,
                () =>
                    !_animeService.Animes.Any(
                        a => a.Name.ToLower().Trim().Equals(Anime?.Name?.ToLower().Trim()))
                    && Anime?.Name?.Length > 0
            );
            return this;
        }

        // 

        public Visibility HasIdOrTotal => Anime.Details.HasId || Anime.Details.TotalEpisodes > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        public static IEnumerable<Status> Statuses => Enum.GetValues(typeof(Status)).Cast<Status>();

        public IAnimeRepository AnimeRepository { get; set; }

        public ISettingsRepository SettingsRepository { get; set; }

        public string Text { get; set; } = "";

        public string Image { get; set; } = "";

        public bool Editing { get; set; }

        // 

        public RelayCommand Command { get; set; }

        public RelayCommand ExitCommand { get; set; }

        public RelayCommand NextCommand { get; set; }

        public RelayCommand PreviousCommand { get; set; }

        public RelayCommand ClearSubgroupCommand => new RelayCommand(() => Anime.PreferredSubgroup = null);

        public DetailsBarViewModel DetailsBar { get; set; }

        public Anime Anime { get; set; }

        public DayOfWeek? AirDay { get; set; }

        // 

        private async void SetAirDay()
        {
            var currentSeason = await FindSeasonAnimeService.New(AnimeSeason.Current, None);
            currentSeason
                .FirstOrNone(anime => anime.Id == Anime.Details.AniId)
                .FlatMap(anime => anime.StartDate.SomeNotNull())
                .Map(startDate => new DateTime(startDate.Year ?? 0, startDate.Month ?? 0, startDate.Day ?? 0, 0, 0, 0, 0))
                .Map(dateTime => dateTime.DayOfWeek)
                .Match(
                    some: day => AirDay = day,
                    none: ()  => AirDay = null
                );
        }

        private void AnimeOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case "Name":
                    Command.RaiseCanExecuteChanged();
                    break;
                case "Episode":
                case "Status":
                case "Rating":
                case "Notes":
                    Anime.Details.NeedsUpdating = true;
                    break;
                default:
                    break;
            }

            _changeMade = true;
        }

        private void SetupImage()
        {
            if (File.Exists(Anime.Details.Image))
                Image = Anime.Details.Image;

            else if (Anime.Details.HasId)
            {
                if (Anime.Details.Image.Contains("https://"))
                    DownloadImage();
                else
                    Image = "../../../Resources/Images/default.png";
            }

            else
                Image = "../../../Resources/Images/default.png";
        }

        private async void DownloadImage()
        {
            var image = Anime.Details.Image;
            var downloadPath = Path.Combine(App.Path.Directory.Images, $"{Anime.Details.Id}.png");

            try
            {
                if (!File.Exists(downloadPath))
                    await Downloader.DownloadFileTaskAsync(image, downloadPath);

                // The download failed; something bad happened, replace with default
                if (new FileInfo(downloadPath).Length / 1024 <= 15)
                {
                    File.Delete(downloadPath);
                    Anime.Details.Image = "../../../Resources/Images/default.png";
                }

                else
                    Anime.Details.Image = downloadPath;

                Image = Anime.Details.Image;
            }

            catch
            {
                Image = "../../../Resources/Images/default.png";
            }
        }

        private void Edit()
        {
            AnimeRepository.Save();
            MessengerInstance.Send(Display.Anime);
        }

        private void Create()
        {
            _animeService.Add(Anime);
            MessengerInstance.Send(ViewRequest.Refresh);
            MessengerInstance.Send(Display.Anime);
        }

        private void CreateAndReturn()
        {
            _animeService.Add(Anime);
            MessengerInstance.Send(Display.Anime);
            MessengerInstance.Send(ViewRequest.Refresh);
            MessengerInstance.Send(Display.Discover);
        }

        private void Next()
        {
            if (_changeMade)
            {
                AnimeRepository.Save();
                _changeMade = false;
            }
            var animes = _animeService.FilteredAndSorted().ToList();
            var anime = animes.First(an => an.Name.Equals(Anime.Name));
            var position = (animes.IndexOf(anime) + 1) % animes.Count;
            MessengerInstance.Send(animes.ElementAt(position));
        }

        private void Previous()
        {
            if (_changeMade)
            {
                AnimeRepository.Save();
                _changeMade = false;
            }
            var animes = _animeService.FilteredAndSorted().ToList();
            var match = animes.First(anime => anime.Name == Anime.Name);
            var position = animes.IndexOf(match) - 1 >= 0 ? animes.IndexOf(match) - 1 : animes.Count - 1;
            MessengerInstance.Send(animes.ElementAt(position));
        }
    }
}