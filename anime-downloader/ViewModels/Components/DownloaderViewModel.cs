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

        private readonly ISettingsService _settings;

        private readonly IFileService _fileService;

        private readonly IAnimeService _animeService;

        private readonly IDownloadService _downloadService;

        public DownloaderViewModel(ISettingsService settings, IFileService fileService, 
                                   IAnimeService animeService, IDownloadService downloadService)
        {
            _settings = settings;
            _fileService = fileService;
            _animeService = animeService;
            _downloadService = downloadService;
        }
        
        public string Text
        {
            get => _text;
            set => Set(() => Text, ref _text, value);
        }

        public async void Download(RadioModel<DownloadOption> radio)
        {
            Text = "";

            if (!_settings.CrucialDirectoriesExist())
            {
                Text = ">> Not all paths have been correctly configured.";
                return;
            }

            MessengerInstance.Send(new WorkMessage {Working = true});

            if (await _downloadService.ServiceAvailable())
                try
                {
                    switch (radio.Data)
                    {
                        case DownloadOption.Next:
                            await CheckForLatestAsync();
                            break;
                        case DownloadOption.Continually:
                            await GetUpToDateAsync();
                            break;
                        case DownloadOption.Missing:
                            await GetMissingEpisodesAsync();
                            break;
                    }
                }

                catch (Exception)
                {
                    Text += ">> An error occured while attempting to download, try again.";
                }

            else
                Text += $">> {_downloadService.ServiceName} is currently offline. Try checking later.";

            MessengerInstance.Send(new WorkMessage {Working = false});
        }

        /// <summary>
        ///     Downloader (Check for latest anime)
        /// </summary>
        private async Task CheckForLatestAsync()
        {
            var animes = _animeService.AiringAndWatchingAndNotCompleted().ToList();

            if (animes.Any())
            {
                Text = ">> Searching for currently airing anime episodes ...\n";
                var downloaded = await _downloadService.DownloadAll(animes, AddToText);
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
                Text = ">> Attempting to catch up on airing anime episodes ...\n";
                foreach (var anime in _animeService.AiringAndWatchingAndNotCompleted())
                {
                    bool downloaded;
                    do
                    {
                        var links = await _downloadService.FindAllMedia(anime, anime.NextEpisode);
                        downloaded = await _downloadService.AttemptDownload(anime, links, AddToText);
                        if (downloaded)
                            total++;
                    } while (downloaded);
                }

                Text += total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.";
            }

            else if (response == MessageBoxResult.No)
                MessengerInstance.Send(ViewDisplay.Download);
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
            Text = ">> Finding all missing episodes ...\n";

            var all = (await _fileService.GetEpisodesAsync(EpisodeStatus.All)).ToList();
            var first = await Task.Run(() => _fileService.FirstEpisodes(all).OrderBy(a => a.Name));
            var last = await Task.Run(() => _fileService.LastEpisodes(all).OrderBy(a => a.Name));
            var ranges = await Task.Run(() => first.Zip(last, (a, b) => new AnimeFileRange(a, b)));
            var total = await _downloadService.DownloadAll(_animeService.AiringAndWatching, ranges, all, AddToText);
            Text += total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.";
        }

        private void AddToText(string text) => Text += text + "\n";
    }
}