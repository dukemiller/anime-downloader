using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels.Components
{
    public class DownloaderViewModel : ViewModelBase
    {
        private string _text;

        public DownloaderViewModel(ISettingsService settings, IAnimeAggregateService animeAggregate, RadioModel radio)
        {
            Text = "";
            Settings = settings;
            AnimeAggregate = animeAggregate;
            Download(radio);
        }

        private ISettingsService Settings { get; }

        private IAnimeAggregateService AnimeAggregate { get; }

        public string Text
        {
            get { return _text; }
            set { Set(() => Text, ref _text, value); }
        }

        private async void Download(RadioModel radio)
        {
            if (!Settings.CrucialDirectoriesExist())
            {
                Text += ">> Not all paths have been correctly configured.";
                return;
            }

            MessengerInstance.Send(new WorkMessage {Working = true});

            if (await AnimeAggregate.DownloadService.ServiceAvailable())
                try
                {
                    if (radio.Tag.Equals("Next"))
                        await CheckForLatestAsync();

                    else if (radio.Tag.Equals("Continually"))
                        await GetUpToDateAsync();

                    else if (radio.Tag.Equals("Missing"))
                        await GetMissingEpisodesAsync();
                }

                catch (Exception)
                {
                    Text += ">> An error occured while attempting to download, try again.";
                }

            else
                Text += $">> {AnimeAggregate.DownloadService.ServiceName} is currently offline. Try checking later.";

            MessengerInstance.Send(new WorkMessage {Working = false});
        }

        /// <summary>
        ///     Downloader (Check for latest anime)
        /// </summary>
        private async Task CheckForLatestAsync()
        {
            var animes = AnimeAggregate.AnimeService.AiringAndWatchingAndNotCompleted().ToList();

            if (animes.Any())
            {
                Text += ">> Searching for currently airing anime episodes ...\n";
                var downloaded = await AnimeAggregate.DownloadService.DownloadAsync(animes, s => Text += s + '\n');
                Text += downloaded > 0 ? $">> Found {downloaded} anime downloads." : ">> No new anime found.";
            }

            else
            {
                Text += ">> No animes need to be downloaded.";
            }
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
                Text += ">> Attempting to catch up on airing anime episodes ...\n";
                foreach (var anime in AnimeAggregate.AnimeService.AiringAndWatchingAndNotCompleted())
                {
                    bool downloaded;
                    do
                    {
                        var links = await AnimeAggregate.DownloadService.FindTorrentsAsync(anime, anime.NextEpisode);
                        downloaded = await AnimeAggregate.DownloadService.DownloadEpisodeAsync(links, anime,
                            s => Text += s + '\n');
                        if (downloaded)
                            total++;
                    } while (downloaded);
                }

                Text += total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.";
            }

            else if (response == MessageBoxResult.No)
            {
                MessengerInstance.Send(Enums.Views.Download);
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
            Text += ">> Finding all missing episodes ...\n";

            var allEpisodeFiles =
                (await AnimeAggregate.FileService.GetEpisodesAsync(EpisodeStatus.All)).ToList();

            var firstEpisodeFiles =
                await Task.Run(() => AnimeAggregate.FileService.FirstEpisodes(allEpisodeFiles).OrderBy(a => a.Name));

            var lastEpisodeFiles =
                await Task.Run(() => AnimeAggregate.FileService.LastEpisodes(allEpisodeFiles).OrderBy(a => a.Name));

            var animeFileRanges =
                await Task.Run(() => firstEpisodeFiles.Zip(lastEpisodeFiles, (a, b) => new AnimeFileRange(a, b)));

            var total =
                await
                    AnimeAggregate.DownloadService.DownloadAsync(AnimeAggregate.AnimeService.AiringAndWatching,
                        animeFileRanges,
                        allEpisodeFiles, s => Text += s + '\n');

            Text += total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.";
        }
    }
}