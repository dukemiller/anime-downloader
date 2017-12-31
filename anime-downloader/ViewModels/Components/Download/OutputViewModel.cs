using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;

namespace anime_downloader.ViewModels.Components.Download
{
    public class OutputViewModel : ViewModelBase
    {
        private string _text;

        private readonly ISettingsRepository _settings;

        private readonly IFileService _fileService;

        private readonly IAnimeService _animeService;

        private readonly IDownloadService _downloadService;

        public OutputViewModel(ISettingsRepository settings, IFileService fileService,
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

            if (!await _settings.CrucialDirectoriesExist())
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

                catch (Exception exception)
                {
                    if (exception is WebException webException
                        && webException.Status == WebExceptionStatus.ProtocolError
                        && Regex.IsMatch(webException.Message, @"\(5\d{2}\)"))
                        Text += ">> The server returned an internal error. Try again in a bit.";

                    else
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

                if (downloaded > 0)
                    MessengerInstance.Send("update");

                Text += downloaded > 0 
                    ? $">> Found {downloaded} anime downloads." 
                    : ">> No new anime found.";
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
            if (await Methods.QuestionYesNo("You could potentially download an entire wrong series if\n" +
                                            "the intended series isn't found by your anime name and \n" +
                                            "settings. Be sure everything on your list retrieves the\n" +
                                            "show you intend.\n\n" +
                                            "Are you sure you want to continue?"))
            {
                var total = 0;
                Text = ">> Attempting to catch up on airing anime episodes ...\n";
                foreach (var anime in _animeService.AiringAndWatchingAndNotCompleted())
                {
                    bool downloaded;
                    do
                    {
                        var links = await _downloadService.FindAllMedia(anime, anime.NextEpisode);
                        downloaded = await _downloadService.AttemptDownload(anime, anime.NextEpisode, links, AddToText);
                        if (downloaded)
                        {
                            total++;
                            anime.Episode++;
                            anime.Details.NeedsUpdating = true;
                        }
                    } while (downloaded);
                }
                var plural = total > 1 ? "downloads" : "download";
                Text += total > 0 ? $">> Found {total} anime {plural}." : ">> No new anime found.";
            }

            else 
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

            var animes = new Dictionary<Anime, List<int>>();

            foreach (var anime in _animeService.AiringAndWatchingAndNotCompleted())
            {
                var files = (await _fileService.GetEpisodesAsync(anime, EpisodeStatus.All)).ToList();
                var (first, last) = (files.FirstOrDefault()?.Episode, files.LastOrDefault()?.Episode);

                // No range check needed or something weird happened
                if (!first.HasValue || !last.HasValue || first == last)
                    continue;

                for (var episode = first.Value; episode <= last.Value; episode++)
                    if (files.All(file => file.Episode != episode))
                    {
                        if (!animes.ContainsKey(anime))
                            animes[anime] = new List<int>();
                        animes[anime].Add(episode);
                    }
            }

            var total = await _downloadService.DownloadSpecificEpisodes(animes, AddToText);
            Text += total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.";
        }

        private void AddToText(string text) => Text += text + "\n";
    }
}