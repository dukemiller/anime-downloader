using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using HtmlAgilityPack;

namespace anime_downloader.ViewModels
{
    public class WebViewModel : ViewModelBase
    {
        private string _searchbox;

        private string _syncText;

        private bool _upToDate;

        private bool _works;

        private readonly IAnimeService _animeService;

        private readonly IMyAnimeListService _malService;

        private readonly IMyAnimeListApi _api;

        public WebViewModel(ISettingsService settingsService, IAnimeService animeService, 
                           IMyAnimeListService malService, IMyAnimeListApi api)
        {
            SettingsService = settingsService;
            _animeService = animeService;
            _malService = malService;
            _api = api;

            // 

            Works = SettingsService.MyAnimeListConfig.Works;
            UpToDate = _animeService.Animes.Any() && !_animeService.NeedsUpdates.Any();
            SyncText = UpToDate ? "Synced" : "Sync";

            // 

            MyAnimeListCommand = new RelayCommand(() => Process.Start("http://myanimelist.net/"));
            AnichartCommand = new RelayCommand(() => Process.Start("http://anichart.net/"));
            NyaaCommand = new RelayCommand(() => Process.Start("https://www.nyaa.se/"));

            ProfileCommand = new RelayCommand(
                () => Process.Start($"http://myanimelist.net/profile/{SettingsService.MyAnimeListConfig.Username}"),
                () => SettingsService.MyAnimeListConfig.Works
            );

            SyncCommand = new RelayCommand(
                Sync,
                () => SettingsService.MyAnimeListConfig.Works
            );

            SearchCommand = new RelayCommand(Search);
            SearchFirstResultCommand = new RelayCommand(SearchFirstResult);
            LoginCommand = new RelayCommand(Login);
            UsageNotesCommand = new RelayCommand(UsageNotes);
            ImportCommand = new RelayCommand(
                Import, 
                () => SettingsService.MyAnimeListConfig.Works);

            // Only called by the tray
            MessengerInstance.Register<NotificationMessage>(this, _ =>
            {
                if (_.Notification.Equals("tray_sync"))
                    SyncCommand.Execute(1);
            });
        }

        // 

        public ISettingsService SettingsService { get; set; }

        private DateTime WaitDelay { get; set; } = DateTime.Now;

        public bool UpToDate
        {
            get { return _upToDate; }
            set { Set(() => UpToDate, ref _upToDate, value); }
        }

        public string SyncText
        {
            get { return _syncText; }
            set { Set(() => SyncText, ref _syncText, value); }
        }

        public string Searchbox
        {
            get { return _searchbox; }
            set { Set(() => Searchbox, ref _searchbox, value); }
        }

        public bool Works
        {
            get { return _works; }
            set
            {
                Set(() => Works, ref _works, value);
                SettingsService.MyAnimeListConfig.Works = value;
                SettingsService.Save();
            }
        }

        public RelayCommand ProfileCommand { get; set; }

        public RelayCommand MyAnimeListCommand { get; set; }

        public RelayCommand AnichartCommand { get; set; }

        public RelayCommand NyaaCommand { get; set; }

        public RelayCommand SyncCommand { get; set; }

        public RelayCommand ImportCommand { get; set; }

        public RelayCommand SearchCommand { get; set; }

        public RelayCommand SearchFirstResultCommand { get; set; }

        public RelayCommand LoginCommand { get; set; }

        public RelayCommand UsageNotesCommand { get; set; }

        // 

        private void RaiseCommandExecutions()
        {
            LoginCommand.RaiseCanExecuteChanged();
            ProfileCommand.RaiseCanExecuteChanged();
            SyncCommand.RaiseCanExecuteChanged();
            ImportCommand.RaiseCanExecuteChanged();
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
            if (DateTime.Now < WaitDelay)
                return;

            MessengerInstance.Send(new WorkMessage {Working = true});
            Works = await _api.VerifyCredentialsAsync();
            MessengerInstance.Send(new WorkMessage {Working = false});
            RaiseCommandExecutions();
            WaitDelay = DateTime.Now.AddSeconds(5);
        }

        private async void Import()
        {
            MessengerInstance.Send(new WorkMessage { Working = true });
            var animes = await Task.Run(async () => await _malService.GetProfileAnime());
            foreach (var anime in animes)
            {
                if (!_animeService.Animes.Any(a => a.MyAnimeList.Id.Equals(anime.MyAnimeList.Id)))
                    _animeService.Add(anime);
            }
            MessengerInstance.Send(new WorkMessage { Working = false });
        }

        private async void SearchFirstResult()
        {
            var text = Searchbox?.Trim();
            if (text?.Length > 0)
            {
                MessengerInstance.Send(new WorkMessage {Working = true});
                await SearchAndOpenAsync(text);
                MessengerInstance.Send(new WorkMessage {Working = false});
            }
        }

        private void Search()
        {
            var text = Searchbox?.Trim();
            if (text?.Length > 0)
            {
                var q = HttpUtility.UrlEncode(text);
                Process.Start($"http://myanimelist.net/anime.php?q={q}");
            }
        }

        private async void Sync()
        {
            SyncText = "Syncing ...";
            MessengerInstance.Send(new WorkMessage {Working = true});
            await _malService.Synchronize();
            MessengerInstance.Send(new WorkMessage {Working = false});
            RaiseCommandExecutions();
            UpToDate = _animeService.Animes.Any() && !_animeService.NeedsUpdates.Any();
            SyncText = UpToDate ? "Synced" : "Sync";
        }

        public static async Task SearchAndOpenAsync(string text)
        {
            var q = HttpUtility.UrlEncode(text);
            var document = new HtmlDocument();

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(new Uri($"https://myanimelist.net/anime.php?q={q}"));
                document.LoadHtml(html);
            }

            var link = document.DocumentNode?
                .SelectSingleNode("//div[@class=\"js-categories-seasonal js-block-list list\"]/table/tr[2]/td[1]")?
                .Descendants("a")?
                .FirstOrDefault();

            if (link != null)
                Process.Start(link.Attributes["href"].Value);
            else
                Methods.Alert("No results found.");
        }
    }
}