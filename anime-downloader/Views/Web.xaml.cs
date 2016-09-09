using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Input;
using anime_downloader.Classes;
using anime_downloader.Classes.Web.MyAnimeList;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Web.xaml
    /// </summary>
    public partial class Web
    {
        private DateTime WaitDelay { get; set; } = DateTime.Now;

        public Web()
        {
            InitializeComponent();
            UpToDate();
        }

        private void UpToDate()
        {
            // TODO: figure out how to add this to the datacontext later
            var upToDate = !MainWindow.Window.AnimeCollection.Animes
                .Any(a => a.MyAnimeList.NeedsUpdating && !a.Status.Equals("On Hold"));

            SyncedUp.Content = upToDate ? "✓" : "✗";
            SyncedUp.Foreground = upToDate ? Color.Green.ToBrush() : Color.Red.ToBrush();

            if (MainWindow.Window.Settings.MyAnimeList.Works)
            {
                // Dont need to click sync if you're up to date
                SyncButton.IsHitTestVisible = !upToDate;
                SyncButton.Opacity = upToDate ? 0.6 : 1.0;
            }

            MyAnimeListGroupbox.DataContext = MainWindow.Window.Settings.MyAnimeList;

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

        private void UsageButton_OnClick(object sender, RoutedEventArgs e)
        {
            HelperMethods.Alert("There are a few tricks and quirks to correctly use the synchronization: \n\n" +

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

        private async void FirstResultButton_OnClick(object sender, RoutedEventArgs e)
        {
            var text = SearchTextBox.Text.Trim();
            if (text.Length > 0)
            {
                MainWindow.Window.ToggleButtons();
                await WebPage.SearchAndOpenAsync(text);
                MainWindow.Window.ToggleButtons();
            }
            
        }

        private void ProfileButton_OnClick(object sender, RoutedEventArgs e) => Process.Start($"http://myanimelist.net/profile/{UsernameTextbox.Text}");

        private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            var text = SearchTextBox.Text.Trim();
            if (text.Length > 0)
            {
                var q = HttpUtility.UrlEncode(text);
                Process.Start($"http://myanimelist.net/anime.php?q={q}");
            }
        }

        private async void LoginButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (DateTime.Now < WaitDelay)
                return;

            WaitDelay = DateTime.Now.AddSeconds(5);
            var credentials = Api.GetCredentials(MainWindow.Window.Settings);
            var result = await Api.VerifyAsync(credentials);
            var temp = MainWindow.Window.Settings.MyAnimeList.Works;
            MainWindow.Window.Settings.MyAnimeList.Works = result;

            if (temp != MainWindow.Window.Settings.MyAnimeList.Works)
                MainWindow.Window.Cycle(MainWindow.Window.Web);
        }

        private async void SyncButton_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Window.ToggleButtons();

            // Get credentials
            var credentials = Api.GetCredentials(MainWindow.Window.Settings);

            // for every anime that needs updating
            foreach (var anime in MainWindow.Window.AnimeCollection.NeedsUpdates)
            {
                // if no id is found
                if (anime.MyAnimeList.Id.IsBlank())
                {
                    if (await Synchronizer.GetId(anime, credentials))
                        await Synchronizer.AddMal(anime, credentials);
                }

                else
                    await Synchronizer.UpdateMal(anime, credentials);
            }

            MainWindow.Window.ToggleButtons();
            MainWindow.Window.Cycle(MainWindow.Window.Web);
        }
    }
}