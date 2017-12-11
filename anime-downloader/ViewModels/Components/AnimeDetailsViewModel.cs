using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.ViewModels.Components
{
    public class AnimeDetailsViewModel : ViewModelBase
    {
        private static readonly WebClient Downloader = new WebClient();
        private ISettingsRepository _settingsRepository;
        private readonly IAnimeService _animeService;
        private IAnimeRepository _animeRepository;
        private Anime _anime;
        private RelayCommand _command;
        private DetailsBarViewModel _detailsBar;
        private string _text;
        private string _image;
        private bool _changeMade;
        private bool _editing;

        // 

        public AnimeDetailsViewModel(IAnimeRepository animeRepository, ISettingsRepository settingsRepository, IAnimeService animeService)
        {
            AnimeRepository = animeRepository;
            SettingsRepository = settingsRepository;
            _animeService = animeService;

            // Behaviors that are the same no matter what condition
            ClearSubgroupCommand = new RelayCommand(() => Anime.PreferredSubgroup = null);
        }

        // 

        public AnimeDetailsViewModel EditExisting(Anime anime)
        {
            Editing = true;
            Anime = anime;
            SetupImage();

            ExitCommand = new RelayCommand(() =>
            {
                AnimeRepository.Save();
                MessengerInstance.Send(ViewDisplay.Anime);
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

            return this;
        }

        public AnimeDetailsViewModel CreateNew()
        {
            Editing = false;
            Anime = new Anime
            {
                Episode = 0,
                Status = Status.Watching,
                Resolution = "720",
                Airing = true,
                Details = { NeedsUpdating = true }
            };

            Image = "../../Resources/Images/default.png";

            ExitCommand = new RelayCommand(() => MessengerInstance.Send(ViewDisplay.Anime));

            // Button
            Text = "Add";
            Command = new RelayCommand(
                Create,
                () =>
                    !_animeService.Animes.Any(
                        a => a.Name.ToLower().Trim().Equals(Anime?.Name?.ToLower().Trim()))
                    && Anime?.Name?.Length > 0
            );

            return this;
        }

        public AnimeDetailsViewModel CreateNewFromAiring(AiringAnime airing)
        {
            Editing = false;
            Anime = new Anime
            {
                Name = airing.TitleEnglish,
                Episode = 0,
                Status = Status.Watching,
                Resolution = "720",
                Airing = true,
                Details =
                {
                    AniId = airing.Id,
                    NeedsUpdating = true,
                    Image = airing.ImagePath,
                    Synopsis = airing.Description,
                    Title = airing.TitleRomaji,
                    English = airing.TitleEnglish,
                    Aired = airing.AnimeSeason,
                    TotalEpisodes = airing.TotalEpisodes,
                    OverallTotal = airing.TotalEpisodes
                }
            };
            Image = airing.ImagePath;

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

        public IAnimeRepository AnimeRepository
        {
            get => _animeRepository;
            set => Set(() => AnimeRepository, ref _animeRepository, value);
        }

        public ISettingsRepository SettingsRepository
        {
            get => _settingsRepository;
            set => Set(() => SettingsRepository, ref _settingsRepository, value);
        }

        public string Text
        {
            get => _text;
            set => Set(() => Text, ref _text, value);
        }

        public string Image
        {
            get => _image;
            set => Set(() => Image, ref _image, value);
        }

        public bool Editing
        {
            get => _editing;
            set => Set(() => Editing, ref _editing, value);
        }

        // 

        public RelayCommand Command
        {
            get => _command;
            set => Set(() => Command, ref _command, value);
        }

        public RelayCommand ExitCommand { get; set; }

        public RelayCommand NextCommand { get; set; }

        public RelayCommand PreviousCommand { get; set; }

        public RelayCommand ClearSubgroupCommand { get; set; }

        public DetailsBarViewModel DetailsBar
        {
            get => _detailsBar;
            set => Set(() => DetailsBar, ref _detailsBar, value);
        }

        public Anime Anime
        {
            get => _anime;
            set => Set(() => Anime, ref _anime, value);
        }

        // 

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
                    Image = "../../Resources/Images/default.png";
            }

            else
                Image = "../../Resources/Images/default.png";
        }

        private async void DownloadImage()
        {
            var image = Anime.Details.Image;
            var downloadPath = Path.Combine(Repositories.SettingsRepository.ImageDirectory, $"{Anime.Details.Id}.png");

            try
            {
                if (!File.Exists(downloadPath))
                    await Downloader.DownloadFileTaskAsync(image, downloadPath);

                // The download failed; something bad happened, replace with default
                if (new FileInfo(downloadPath).Length / 1024 <= 15)
                {
                    File.Delete(downloadPath);
                    Anime.Details.Image = "../../Resources/Images/default.png";
                }

                else
                    Anime.Details.Image = downloadPath;

                Image = Anime.Details.Image;
            }

            catch
            {
                Image = "../../Resources/Images/default.png";
            }
        }

        private void Edit()
        {
            AnimeRepository.Save();
            MessengerInstance.Send(ViewDisplay.Anime);
        }

        private void Create()
        {
            _animeService.Add(Anime);
            MessengerInstance.Send(ViewDisplay.Anime);
        }

        private void CreateAndReturn()
        {
            _animeService.Add(Anime);
            MessengerInstance.Send("refresh");
            MessengerInstance.Send(ViewDisplay.Discover);
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
            var anime = animes.First(an => an.Name.Equals(Anime.Name));
            var position = animes.IndexOf(anime) - 1 >= 0 ? animes.IndexOf(anime) - 1 : animes.Count - 1;
            MessengerInstance.Send(animes.ElementAt(position));
        }
    }
}