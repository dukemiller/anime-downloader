using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using anime_downloader.Annotations;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Web.xaml
    /// </summary>
    public partial class Web : INotifyPropertyChanged
    {

        private DateTime WaitDelay { get; set; } = DateTime.Now;

        private bool _upToDate;

        public bool UpToDate
        {
            get { return _upToDate; }
            set
            {
                _upToDate = value;
                OnPropertyChanged();
            }
        }

        private MyAnimeListLoginDetails _loginDetails;

        public MyAnimeListLoginDetails LoginDetails
        {
            get { return _loginDetails; }
            set
            {
                _loginDetails = value; 
                OnPropertyChanged();
            }
        }

        private string _syncText;

        public string SyncText
        {
            get { return _syncText; }
            set
            {
                _syncText = value;
                OnPropertyChanged();
            }
        }

        public Web()
        {
            UpToDate = !MainWindow.Window.AnimeCollection.NeedsUpdates.Any();
            LoginDetails = MainWindow.Window.Settings.MyAnimeList;
            SyncText = UpToDate ? "Synced" : "Sync";
            InitializeComponent();
        }

        private void MyanimelistButton_Click(object sender, RoutedEventArgs e) => Process.Start("http://myanimelist.net/");

        private void AnichartButton_Click(object sender, RoutedEventArgs e) => Process.Start("http://anichart.net/");

        private void NyaaButton_Click(object sender, RoutedEventArgs e) => Process.Start("https://www.nyaa.se/");

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchButton.Press();
        }

        private void SearchButton_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SearchTextBox.Text.Trim();
            if (text.Length > 0)
            {
                var q = HttpUtility.ParseQueryString(text);
                Process.Start($"http://myanimelist.net/anime.php?q={q}");
            }
        }

        // 

        private void UsageNotes_OnClick(object sender, RoutedEventArgs e)
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

        private void GotoProfile_OnClick(object sender, RoutedEventArgs e) => Process.Start($"http://myanimelist.net/profile/{LoginDetails.Username}");

        private void Search_OnClick(object sender, RoutedEventArgs e)
        {
            var text = SearchTextBox.Text.Trim();
            if (text.Length > 0)
            {
                var q = HttpUtility.UrlEncode(text);
                Process.Start($"http://myanimelist.net/anime.php?q={q}");
            }
        }

        private async void Search_FirstResult_OnClick(object sender, RoutedEventArgs e)
        {
            var text = SearchTextBox.Text.Trim();
            if (text.Length > 0)
            {
                MainWindow.Window.GetAll<ToggleButton>().Toggle();
                await WebPage.SearchAndOpenAsync(text);
                MainWindow.Window.GetAll<ToggleButton>().Toggle();
            }

        }

        private async void Login_OnClick(object sender, RoutedEventArgs e)
        {
            if (DateTime.Now < WaitDelay)
                return;

            WaitDelay = DateTime.Now.AddSeconds(5);
            var credentials = Api.GetCredentials(MainWindow.Window.Settings);
            var result = await Api.VerifyCredentialsAsync(credentials);
            var temp = MainWindow.Window.Settings.MyAnimeList.Works;
            MainWindow.Window.Settings.MyAnimeList.Works = result;

            if (temp != MainWindow.Window.Settings.MyAnimeList.Works)
                MainWindow.Window.Cycle(MainWindow.Window.Web);
        }

        private async void Sync_OnClick(object sender, RoutedEventArgs e)
        {
            SyncText = "Syncing ...";
            MainWindow.Window.GetAll<ToggleButton>().Toggle();
            await Synchronizer.FullSynchronize();
            MainWindow.Window.GetAll<ToggleButton>().Toggle();
            UpToDate = !MainWindow.Window.AnimeCollection.NeedsUpdates.Any();
            SyncText = UpToDate ? "Synced" : "Sync";
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}