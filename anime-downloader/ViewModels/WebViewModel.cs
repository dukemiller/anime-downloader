using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using anime_downloader.Views.Dialogs;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MaterialDesignThemes.Wpf;

namespace anime_downloader.ViewModels
{
    public class WebViewModel : ViewModelBase
    {
        private string _searchbox;

        private bool _synced;

        private bool _loggedIn;

        private readonly ICredentialsRepository _credentialsRepository;

        private readonly IAnimeRepository _animeRepository;

        private readonly IAnimeService _animeService;

        private readonly ISyncProviderService _syncService;

        private readonly IMyAnimeListApi _api;

        public IDownloadService DownloadService { get; }

        private string _synchronize;

        private string _log;

        private RelayCommand _logCommand;

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

            SetCommands();
            CheckSyncAndLog();

            MessengerInstance.Register<NotificationMessage>(this, _ =>
            {
                if (_.Notification.Equals("tray_sync"))
                    SyncCommand.Execute(1);
            });

            MessengerInstance.Register<string>(this, _ =>
            {
                switch (_)
                {
                    case "reset":
                        CheckSyncAndLog();
                        break;
                    default:
                        break;
                }
            });
        }

        // 

        private DateTime LoginAttempt { get; set; } = DateTime.Now;

        public bool Synced
        {
            get => _synced;
            set => Set(() => Synced, ref _synced, value);
        }

        public bool LoggedIn
        {
            get => _loggedIn;
            set => Set(() => LoggedIn, ref _loggedIn, value);
        }

        public string Searchbox
        {
            get => _searchbox;
            set => Set(() => Searchbox, ref _searchbox, value);
        }

        public string Synchronize
        {
            get => _synchronize;
            set => Set(() => Synchronize, ref _synchronize, value);
        }

        public string Log
        {
            get => _log;
            set => Set(() => Log, ref _log, value);
        }

        // 

        public RelayCommand<string> OpenUrlCommand { get; set; }

        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand SyncCommand { get; set; }

        public RelayCommand ImportCommand { get; set; }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand SearchFirstResultCommand { get; set; }

        public RelayCommand LogCommand
        {
            get => _logCommand;
            set => Set(() => LogCommand, ref _logCommand, value);
        }

        public RelayCommand UsageNotesCommand { get; set; }

        // 

        private void SetCommands()
        {
            // Open website
            OpenUrlCommand = new RelayCommand<string>(OpenUrl);

            // Just text

            UsageNotesCommand = new RelayCommand(UsageNotes);

            // Searches

            SearchCommand = new RelayCommand(Search);
            SearchFirstResultCommand = new RelayCommand(SearchFirstResult);

            // MyAnimeList

            ProfileCommand = new RelayCommand(
                () => Process.Start($"http://myanimelist.net/profile/{_credentialsRepository.MyAnimeListConfig.Username}"),
                () => _credentialsRepository.MyAnimeListConfig.LoggedIn
            );

            ImportCommand = new RelayCommand(
                Import, 
                () => _credentialsRepository.MyAnimeListConfig.LoggedIn
            );

            SyncCommand = new RelayCommand(
                Sync, 
                () => _credentialsRepository.MyAnimeListConfig.LoggedIn && !Synced
            );
        }

        private void RaiseCommandExecutions()
        {
            ProfileCommand.RaiseCanExecuteChanged();
            SyncCommand.RaiseCanExecuteChanged();
            ImportCommand.RaiseCanExecuteChanged();
        }

        private void CheckSyncAndLog()
        {
            LoggedIn = _credentialsRepository.MyAnimeListConfig.LoggedIn;

            if (LoggedIn)
            {
                Log = "Log out";
                LogCommand = new RelayCommand(Logout);
            }

            else
            {
                Log = "Log in";
                LogCommand = new RelayCommand(Login);
            }

            Synced = _animeService.Synced;
            Synchronize = Synced ? "Synched" : "Synchronize";

            RaiseCommandExecutions();
        }

        // 

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
                    Process.Start(DownloadService.ServiceUrl);
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
            var view = new MyAnimeListLoginDialog();
            var result = await DialogHost.Show(view) as MyAnimeListConfiguration;
            
            if (result == null || result.Password?.Length < 1 || result.Username?.Length < 1 || DateTime.Now < LoginAttempt)
                return;

            MessengerInstance.Send(new WorkMessage {Working = true});
            _credentialsRepository.MyAnimeListConfig = result;
            _credentialsRepository.MyAnimeListConfig.LoggedIn = await _api.VerifyCredentialsAsync();
            _credentialsRepository.Save();
            MessengerInstance.Send(new WorkMessage {Working = false});

            LoginAttempt = DateTime.Now.AddSeconds(5);
            CheckSyncAndLog();
        }

        private void Logout()
        {
            _credentialsRepository.MyAnimeListConfig = new MyAnimeListConfiguration();
            _credentialsRepository.Save();
            CheckSyncAndLog();
        }

        private async void Import()
        {
            MessengerInstance.Send(new WorkMessage { Working = true });
            var animes = await Task.Run(async () => await _syncService.LoadProfile());
            foreach (var anime in animes)
                if (!_animeService.Animes.Any(a => a.Details.Id.Equals(anime.Details.Id)))
                    _animeService.Add(anime);
            MessengerInstance.Send(new WorkMessage { Working = false });
        }

        private async void SearchFirstResult()
        {
            var text = Searchbox?.Trim();
            if (text?.Length > 0)
            {
                MessengerInstance.Send(new WorkMessage {Working = true});
                await SearchAndOpen(text);
                MessengerInstance.Send(new WorkMessage {Working = false});
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
            MessengerInstance.Send(new WorkMessage {Working = true});
            Synchronize = "Synchronizing";
            await _syncService.Synchronize();
            _animeRepository.Save();
            MessengerInstance.Send("update");
            MessengerInstance.Send(new WorkMessage {Working = false});
            RaiseCommandExecutions();
            CheckSyncAndLog();
        }

        private async Task SearchAndOpen(string text)
        {
            var result = await _syncService.FindProfilePage(text);
            if (result != null)
                Process.Start(result);
            else
                Methods.Alert("No results found.");
        }
    }
}