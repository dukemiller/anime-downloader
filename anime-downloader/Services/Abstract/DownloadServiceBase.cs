using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.Services.Abstract
{
    /// <summary>
    ///     A base class encompassing most of the generic functionality of a downloadservice
    /// </summary>
    /// <remarks>
    ///     Most of the download service functionality is actually very sharable between any particular instance,
    ///     so inheriting classes would have less to manage for implementing download services for other sites
    /// </remarks>
    public abstract class DownloadServiceBase : IDownloadService
    {
        protected WebClient Downloader { get; } = new WebClient();

        // 

        public string Name => GetType().Name.Replace("Service", "");

        public async Task<int> Download(DownloadOption option, Action<string> output)
        {
            var total = 0;

            switch (option)
            {
                case DownloadOption.Next:
                    foreach (var anime in AnimeService.AiringAndWatchingAndNotCompleted)
                        if (await DownloadSingleEpisode(anime, anime.NextEpisode, output))
                            total++;
                    break;

                case DownloadOption.Continually:
                    foreach (var anime in AnimeService.AiringAndWatchingAndNotCompleted)
                    {
                        bool downloaded;
                        do
                        {
                            downloaded = await DownloadSingleEpisode(anime, anime.NextEpisode, output);
                            if (downloaded)
                                total++;
                        } while (downloaded);
                    }

                    break;

                case DownloadOption.Missing:
                    var animes = await MissingAnime();
                    foreach (var anime in animes.Keys)
                    foreach (var episode in animes[anime])
                        if (await DownloadSingleEpisode(anime, episode, output, false))
                            total++;
                        else
                            break;

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(option), option, null);
            }

            if (total > 0)
                AnimeRepository.Save();

            return total;
        }

        public async Task<bool> Available()
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(Url);
                request.Timeout = 3000;
                request.Method = WebRequestMethods.Http.Head;
                request.UserAgent =
                    @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                request.Accept = "*/*";
                request.ContentLength = 0;
                request.Headers = new WebHeaderCollection {"cache-control: no-cache", "accept-encoding: gzip, deflate"};
                using (await request.GetResponseAsync())
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 

        private async Task<List<RemoteMedia>> FindAllMedia(Anime anime, int episode) =>
            (string.IsNullOrEmpty(anime.Details.PreferredSearchTitle)
                ? await DownloadPreference.DetermineSearchTitle(anime, episode)
                : await FindAllMedia(anime, anime.Details.PreferredSearchTitle, episode))
            .Where(CanDownload(anime))
            .ToList();

        private Func<RemoteMedia, bool> CanDownload(Anime anime) => media =>
        {
            if (anime is null || media is null)
                return false;

            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.ToLower().Equals(media.StrippedWithNoEpisode.ToLower()))
                return false;

            // Not the preferred subgroup
            if (!string.IsNullOrEmpty(anime.PreferredSubgroup)
                && !media.Subgroup().Exists(subgroup => subgroup.ToLower().Contains(anime.PreferredSubgroup.ToLower())))
                return false;

            // Not on whitelist
            if (SettingsRepository.FlagConfig.OnlyWhitelisted
                && !media.Subgroup().Exists(group =>
                    SettingsRepository.Subgroups.Select(s => s.ToLower()).Contains(group.ToLower())))
                return false;

            return true;
        };

        private static async Task<bool> Download(Anime anime, RemoteMedia media) =>
            (await MediaManager
                .Download(anime, media)
                .MapAsync(command => Tee(MediaManager.Start, media, command))).HasValue;

        private async Task<bool> DownloadSingleEpisode(Anime anime, int episode, Action<string> output,
            bool update = true)
        {
            var medias = await FindAllMedia(anime, episode);

            if (medias.Count <= 0)
                return false;

            while (Messages.Count > 0)
                output(Messages.Dequeue());

            foreach (var media in medias)
            {
                if (await Download(anime, media))
                {
                    output($"Downloading '{anime.Title}' episode '{media.Episode.ValueOr(0)}'.");

                    await Log(anime, episode);

                    if (update)
                    {
                        anime.Episode++;
                        anime.Details.NeedsUpdating = true;
                    }

                    return true;
                }

                output($"Download of '{anime.Title}' failed (most likely due to server error).");
            }

            return false;
        }

        private static async Task Log(Anime anime, int episode)
        {
            var timestamp = $"{DateTime.Now:[M/d/yyyy @ hh:mm:ss tt]}";
            var message = $"Downloaded '{anime.Title}' episode {episode}.";
            using (var streamWriter = new StreamWriter(App.Path.Logging, true))
                await streamWriter.WriteLineAsync($"{timestamp} - {message}");
        }

        /// <summary>
        ///     Many sites have different ways their search works, but on the nyaa.* sites
        ///     this will transform the title into a searchable query.
        /// </summary>
        protected static string TransformEpisodeSearch(string name, int episode) =>
            $"{TransformEpisodeSearch(name)}+{episode:D2}";

        protected static string TransformEpisodeSearch(string name)
        {
            // The kino no tabi regex
            name = Regex.Replace(name, @"(:\s(\w+\s){3,}\-\s(\w+\s){2,}(\w+\s?))", "");

            // Remove first three words after a colon, usually a title (e.g. shokugeki: san no sara)
            name = Regex.Replace(name, @"(:\s(\w+\s){2,}\w+$)", "");

            // Remove last number of title (usually season number, e.g. Rin-ne 3)
            name = Regex.Replace(name, @"\s\d{1}$", "");

            // Remove specifically '(TV)' meta tags, year meta tags and anything else inside paren is usually significant
            name = Regex.Replace(name, @"\(TV\)", "");

            // Remove literal season declarations from the title
            name = Regex.Replace(name, @"(2nd season|the (?:animation|animated series))", "", RegexOptions.IgnoreCase);

            // Troublesome characters for the search
            name = Regex.Replace(name, @":|/|-|\.", " ");

            // Shorten length of titles that have more than 5 words
            if (name.Split(' ').Length > 5)
                name = string.Join(" ", name.Split(' ').Take(5));

            // Remove duplicate spaces
            name = Regex.Replace(name, @"\s+", " ").Trim();

            // Convert rest
            name = name
                .Replace("'s", "")
                .Replace(" ", "+")
                .Replace("!", "%21")
                .Replace("Souma", "Souma|Soma") // ugly hack
                .Replace("'", "%27");

            return name;
        }

        private async Task<Dictionary<Anime, List<int>>> MissingAnime()
        {
            var animes = new Dictionary<Anime, List<int>>();

            foreach (var anime in AnimeService.AiringAndWatching)
            {
                var files = (await FileService.GetEpisodesAsync(anime, EpisodeStatus.All)).ToList();
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

            return animes;
        }

        // 

        public Queue<string> Messages { get; } = new Queue<string>();

        // Abstract inheritors

        protected abstract ISettingsRepository SettingsRepository { get; }

        protected abstract IAnimeRepository AnimeRepository { get; }

        protected abstract IAnimeService AnimeService { get; }

        protected abstract IFileService FileService { get; }

        public abstract string Url { get; }

        public abstract Task<List<RemoteMedia>> FindAllMedia(Anime anime, string name, int episode);

        public abstract Task<List<RemoteMedia>> PotentialStartingEpisode(string name);
    }
}