using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using anime_downloader.Views.Dialogs.MyAnimeList;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MaterialDesignThemes.Wpf;
using PropertyChanged;

namespace anime_downloader.ViewModels.Displays
{
    public class WebViewModel : ViewModelBase
    {
        private readonly ICredentialsRepository _credentialsRepository;

        private readonly IAnimeRepository _animeRepository;

        private readonly IAnimeService _animeService;

        private readonly ISyncProviderService _syncService;

        private readonly IMyAnimeListApi _api;

        public IDownloadService DownloadService { get; }
        
        public WebViewModel(ICredentialsRepository credentialsRepository,
            IAnimeRepository animeRepository,
            IAnimeService animeService,
            ISyncProviderService syncService,
            IMyAnimeListApi api,
            IDownloadService downloadService)
        {
            DownloadService = downloadService;
            _credentialsRepository = credentialsRepository;
            _animeRepository = animeRepository;
            _animeService = animeService;
            _syncService = syncService;
            _api = api;

            // 

            Synced = _animeService.Synced;
            LoggedIn = _credentialsRepository.MyAnimeListConfig.LoggedIn;
            MessengerInstance.Register<Request>(this, HandleRequest);
        }

        // 

        private DateTime LoginAttempt { get; set; } = DateTime.Now;

        public bool Synced { get; set; }

        public bool LoggedIn { get; set; }

        public bool Syncing { get; set; }

        public string Searchbox { get; set; } = "";

        [DependsOn(nameof(Synced), nameof(Syncing))]
        public string Synchronize => Syncing ? "Synchronizing" : Synced ? "Synched" : "Synchronize";

        [DependsOn(nameof(LoggedIn))]
        public string Log => LoggedIn ? "Log out" : "Log in";

        public RelayCommand<string> OpenUrlCommand => new RelayCommand<string>(OpenUrl);

        public RelayCommand SearchCommand => new RelayCommand(Search);

        public RelayCommand SearchFirstResultCommand => new RelayCommand(SearchFirstResult);

        [DependsOn(nameof(LoggedIn))]
        public RelayCommand LogCommand => LoggedIn ? new RelayCommand(Logout) : new RelayCommand(Login);

        public RelayCommand UsageNotesCommand => new RelayCommand(UsageNotes);

        [DependsOn(nameof(Synced), nameof(LoggedIn))]
        public RelayCommand SyncCommand => new RelayCommand(Sync, () => LoggedIn && !Synced);

        [DependsOn(nameof(LoggedIn))]
        public RelayCommand ImportCommand => new RelayCommand(Import, () => false); // fix this

        [DependsOn(nameof(LoggedIn))]
        public RelayCommand ProfileCommand => new RelayCommand(
            () => Process.Start($"http://myanimelist.net/profile/{_credentialsRepository.MyAnimeListConfig.Username}"),
            () => LoggedIn
        );

        // 

        private void HandleRequest(Request request)
        {
            if (request == Request.TraySynchronize)
                SyncCommand.Execute(1);
        }
        
        private void OpenUrl(string token)
        {
            switch (token)
            {
                case "myanimelist":
                    Process.Start("http://myanimelist.net/");
                    break;
                case "anilist":
                    Process.Start("https://anilist.co/");
                    break;
                case "anichart":
                    Process.Start("http://anichart.net/");
                    break;
                case "provider":
                    Process.Start(DownloadService.Url);
                    break;
                default:
                    break;
            }
        }

        private static void UsageNotes()
        {
            Methods.Alert("There are a few tricks and quirks to correctly use the synchronization: \n\n" +
                          "1. Be partial against using any nicknames for the show, you have a higher chance " +
                          "of finding the show with original english or romaji.\n\n" +
                          "2. OVAs have absolutely no chance of being found, so don't expect them to be " +
                          "found. Anime shorts can still be found if they're the content of the show, " +
                          "i.e. the show itself is only shorts.\n\n" +
                          "3. If the show has a close matching name to another series or is a single " +
                          "word (e.g. GATE vs Steins;Gate), flagging in the anime details for 'name " +
                          "strict' will find exact matches of the show and have a greater chance of " +
                          "correctly tagging the right show.\n\n" +
                          "4. For shows that have a season with another name, try your hardest to " +
                          "maintain that naming by adding a new series and marking the original " +
                          "series as complete instead of keeping the same name and downloading new " +
                          "episodes. It should still work, but it's bound to cause some type of " +
                          "problem."
            );
        }

        private async void Login()
        {
            if (!(await DialogHost.Show(new LoginDialog()) is MyAnimeListConfiguration result) 
                    || result.Password?.Length < 1 
                    || result.Username?.Length < 1 
                    || DateTime.Now < LoginAttempt)
                return;

            MessengerInstance.Send(ViewState.IsWorking);

            if (await _api.Login(result.Username, result.Password))
            {
                _credentialsRepository.MyAnimeListConfig = result;
                _credentialsRepository.MyAnimeListConfig.LoggedIn = true;
                _credentialsRepository.Save();
            }

            MessengerInstance.Send(ViewState.DoneWorking);

            LoginAttempt = DateTime.Now.AddSeconds(5);
            Synced = _animeService.Synced;
            LoggedIn = _credentialsRepository.MyAnimeListConfig.LoggedIn;
        }

        private void Logout()
        {
            _credentialsRepository.MyAnimeListConfig = new MyAnimeListConfiguration();
            _credentialsRepository.Save();
            Synced = _animeService.Synced;
            LoggedIn = _credentialsRepository.MyAnimeListConfig.LoggedIn;
        }

        private async void Import()
        {
            MessengerInstance.Send(ViewState.IsWorking);
            var animes = await Task.Run(async () => await _syncService.LoadProfile());
            foreach (var anime in animes)
                if (!_animeService.Animes.Any(a => a.Details.Id.Equals(anime.Details.Id)))
                    _animeService.Add(anime);
            MessengerInstance.Send(ViewState.DoneWorking);
        }

        private async void SearchFirstResult()
        {
            var text = Searchbox?.Trim();
            if (text?.Length > 0)
            {
                MessengerInstance.Send(ViewState.IsWorking);
                await SearchAndOpen(text);
                MessengerInstance.Send(ViewState.DoneWorking);
            }
        }

        private void Search()
        {
            var text = Searchbox?.Trim();
            if (!(text?.Length > 0))
                return;
            Process.Start($"http://myanimelist.net/anime.php?q={HttpUtility.UrlEncode(text)}");
        }

        private async void Sync()
        {
            MessengerInstance.Send(ViewState.IsWorking);
            Syncing = true;
            try
            {
                await _syncService.Synchronize();
            }
            finally
            {
                _animeRepository.Save();
                Syncing = false;
            }

            MessengerInstance.Send(ViewRequest.Update);
            MessengerInstance.Send(ViewState.DoneWorking);
            Synced = _animeService.Synced;
        }

        private async Task SearchAndOpen(string text)
            => (await _syncService.FindProfilePage(text)).Match(
                some: result => Process.Start(result),
                none: () => Methods.Alert("No results found.")
            );
    }
}