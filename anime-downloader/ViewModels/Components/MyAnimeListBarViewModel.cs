using System.Diagnostics;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class MyAnimeListBarViewModel : ViewModelBase
    {
        private Anime _anime;

        private readonly ICredentialsRepository _credentialsRepository;
        private readonly IAnimeRepository _animeRepository;
        private readonly IMyAnimeListService _malService;
        private bool _loggedIntoMal;
        private bool _hasId;
        private Visibility _hasIdVisibility;

        // 

        public MyAnimeListBarViewModel(ICredentialsRepository credentialsRepository,
            IAnimeRepository animeRepository,
            IMyAnimeListService malService)
        {
            _credentialsRepository = credentialsRepository;
            _animeRepository = animeRepository;
            _malService = malService;

            FindCommand = new RelayCommand(Find, () => _credentialsRepository.MyAnimeListConfig.LoggedIn);
            ClearCommand = new RelayCommand(Clear);
            ProfileCommand = new RelayCommand(Profile);
            RefreshCommand = new RelayCommand(Refresh);

            _credentialsRepository.MyAnimeListConfig.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("LoggedIn"))
                    LoggedIntoMal = _credentialsRepository.MyAnimeListConfig.LoggedIn;
            };
        }

        public MyAnimeListBarViewModel Load(Anime anime)
        {
            _anime = anime;

            LoggedIntoMal = _credentialsRepository.MyAnimeListConfig.LoggedIn;
            HasId = _anime.Details.HasId;
            HasIdVisibility = HasId ? Visibility.Visible : Visibility.Collapsed;

            _anime.Details.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName.Equals("Id"))
                {
                    HasId = _anime.Details.HasId;
                    HasIdVisibility = HasId ? Visibility.Visible : Visibility.Collapsed;
                }
            };

            return this;
        }

        // 

        public bool LoggedIntoMal
        {
            get => _loggedIntoMal;
            set => Set(() => LoggedIntoMal, ref _loggedIntoMal, value);
        }

        public bool HasId
        {
            get => _hasId;
            set => Set(() => HasId, ref _hasId, value);
        }

        public Visibility HasIdVisibility
        {
            get => _hasIdVisibility;
            set => Set(() => HasIdVisibility, ref _hasIdVisibility, value);
        }

        public RelayCommand FindCommand { get; set; }

        public RelayCommand ClearCommand { get; set; }

        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand RefreshCommand { get; set; }

        // 

        private async void Find()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});
            var id = await _malService.GetId(_anime);
            RaisePropertyChanged(nameof(HasIdVisibility));
            _animeRepository.Save();
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
                _anime.Details = new AnimeDetails {Id = null, NeedsUpdating = true};
                RaisePropertyChanged(nameof(HasIdVisibility));
                _animeRepository.Save();
            }
        }

        private void Profile() => Process.Start($"http://myanimelist.net/anime/{_anime.Details.Id}");

        private async void Refresh()
        {
            MessengerInstance.Send(new WorkMessage {Working = true});
            if (!await _malService.Refresh(_anime))
                Methods.Alert("Had trouble finding this show on MAL.");
            RaisePropertyChanged(nameof(HasIdVisibility));
            MessengerInstance.Send(new WorkMessage {Working = false});
        }
    }
}