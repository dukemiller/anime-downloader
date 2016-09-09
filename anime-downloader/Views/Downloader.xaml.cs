using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Classes.File;
using anime_downloader.Classes.Web;
using anime_downloader.Enums;
using MessageBox = System.Windows.MessageBox;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Downloader.xaml
    /// </summary>
    public partial class Downloader
    {
        public Downloader()
        {
            InitializeComponent();
        }

        public async void Logger()
        {
            var text = ">> No downloads have been logged so far.";

            if (File.Exists(Classes.Settings.LoggingFile))
            {
                using (var reader = new StreamReader(Classes.Settings.LoggingFile))
                    text = await reader.ReadToEndAsync();
                text = string.Join("\n", text.Split('\n').Reverse().Skip(1));
            }

            TextBox.Text = text;
        }

        public async void Download(string choice)
        {
            if (!MainWindow.Window.CrucialDirectoriesExist())
                return;

            MainWindow.Window.ToggleButtons();

            if (await Nyaa.IsOnlineAsync())
            {
                try
                {
                    if (choice.Equals("CheckForLatest"))
                        await CheckForLatestAsync();
                    else if (choice.Equals("GetUpToDate"))
                        await GetUpToDateAsync();
                    else if (choice.Equals("GetMissing"))
                        await GetMissingEpisodesAsync();
                }

                catch (Exception)
                {
                    TextBox.WriteLine(">> An error occured while attempting to download, try again.");
                }
            }

            else
            {
                TextBox.WriteLine(">> Nyaa is currently offline. Try checking later.");
            }

            HelperMethods.ClearFocusFrom(TextBox);
            MainWindow.Window.ToggleButtons();
        }
        
        /// <summary>
        ///     Downloader (Check for latest anime)
        /// </summary>
        private async Task CheckForLatestAsync()
        {
            TextBox.WriteLine(">> Searching for currently airing anime episodes ...");
            var downloaded = await MainWindow.Window.Downloader.DownloadAsync(MainWindow.Window.AnimeCollection.AiringAndWatching, TextBox);
            TextBox.WriteLine(downloaded > 0
                ? $">> Found {downloaded} anime downloads."
                : ">> No new anime found.");
        }

        /// <summary>
        ///     Downloader (Get up to date)
        /// </summary>
        private async Task GetUpToDateAsync()
        {
            var response =
                MessageBox.Show(
                    "You could potentially download an entire wrong series if the intended series isn't " +
                    "found by your anime name and settings. Be sure everything on your list retrieves the " +
                    "show you intend. \n\n" +
                    "Are you sure you want to continue?",
                    "Confirmation",
                    MessageBoxButton.YesNo);

            if (response == MessageBoxResult.Yes)
            {
                var total = 0;
                TextBox.WriteLine(">> Attempting to catch up on airing anime episodes ...");
                foreach (var anime in MainWindow.Window.AnimeCollection.AiringAndWatching.ToList())
                {
                    bool downloaded;
                    do
                    {
                        downloaded =
                            await MainWindow.Window.Downloader.DownloadEpisodeAsync(await anime.GetLinksToNextEpisode(), anime, TextBox);
                        if (downloaded)
                            total++;
                    } while (downloaded);
                }

                TextBox.WriteLine(total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.");
            }

            else if (response == MessageBoxResult.No)
            {
                MainWindow.Window.ToggleButtons();
                MainWindow.Window.Download.Press();
                MainWindow.Window.ToggleButtons();
            }
        }

        /// <summary>
        ///     Downloader (Download missing episodes)
        /// </summary>
        /// <remarks>   
        ///     Find and download any episodes in collection anime that are between 
        ///     the range start.episode and last.episode
        /// </remarks>
        private async Task GetMissingEpisodesAsync()
        {
            TextBox.WriteLine(">> Finding all missing episodes ...");

            var allEpisodeFiles =
                (await MainWindow.Window.AnimeFileCollection.GetEpisodesAsync(EpisodeStatus.All)).ToList();

            var firstEpisodeFiles =
                await Task.Run(() => AnimeFileCollection.FirstEpisodesOf(allEpisodeFiles).OrderBy(a => a.Name));

            var lastEpisodeFiles =
                await Task.Run(() => AnimeFileCollection.LastEpisodesOf(allEpisodeFiles).OrderBy(a => a.Name));

            var animeFileRanges =
                await Task.Run(() => firstEpisodeFiles.Zip(lastEpisodeFiles, (a, b) => new AnimeFileRange(a, b)));

            var total =
                await
                    MainWindow.Window.Downloader.DownloadAsync(MainWindow.Window.AllAnime.AiringAndWatching(),
                        animeFileRanges, allEpisodeFiles, TextBox);

            TextBox.WriteLine(total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.");
        }

    }
}