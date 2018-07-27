using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using MagnetLink = anime_downloader.Models.MagnetLink;

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
        private async Task<(bool successful, string path)> DownloadTorrent(Torrent torrent)
        {
            var torrentName = await torrent.GetTorrentNameAsync();
            if (torrentName == null)
                return (false, null);
            var filePath = Path.Combine(SettingsRepository.PathConfig.Torrents, torrentName);

            // Download file 
            if (!File.Exists(filePath))
                return await Task.Run(() =>
                {
                    try
                    {
                        Downloader.DownloadFile(torrent.Remote, filePath);
                    }

                    // TODO: heh heh heh
                    catch (Exception)
                    {
                        return (false, null);
                    }
                    return (true, filePath);
                });

            return (true, filePath);
        }

        private void StartInTorrentClient(string command)
        {
            var info = new ProcessStartInfo
            {
                FileName = SettingsRepository.PathConfig.TorrentDownloader,
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = new Process
            {
                StartInfo = info
            };

            Task.Run(() => process.Start());
        }

        /// <summary>
        ///     The arbitrary "health score" of a list of resources for categorizing if
        ///     trusting this selection of results is more capable of representing what
        ///     people search for
        /// </summary>
        private static int HealthScore(IReadOnlyCollection<RemoteMedia> mediaSource)
        {
            // For any "average" torrent, these would be the stats
            var count = mediaSource.Count;
            if (count == 0)
                return -1;
            var seeders = mediaSource.Select(media => media.Health).Sum() / count;
            var downloads = mediaSource.Select(m => m.Downloads).Sum() / count;
            return (seeders + downloads) * count;
        }
        
        private static List<RemoteMedia> CalculateAndSetPreference(Anime anime, List<RemoteMedia> english, List<RemoteMedia> romanji)
        {
            List<RemoteMedia> media;

            var englishScore = HealthScore(english);
            var romanjiScore = HealthScore(romanji);

            // Set it to romanji, but don't save any titles
            if (englishScore == -1 && romanjiScore == -1)
                media = romanji;
            else if (englishScore > romanjiScore)
            {
                anime.Details.PreferredSearchTitle = anime.Details.English;
                media = english;
            }

            else
            {
                anime.Details.PreferredSearchTitle = anime.Details.Title;
                media = romanji;
            }

            return media;

        }

        /// <summary>
        ///     A one time but potentially expensive cost to find out the preferred name
        /// </summary>
        private async Task<List<RemoteMedia>> GetMedia(Anime anime, int episode)
        {
            List<RemoteMedia> media;

            // If there is both an english and romanji title
            if (!string.IsNullOrEmpty(anime.Details.Title) && !string.IsNullOrEmpty(anime.Details.English))
            {
                if (!string.IsNullOrEmpty(anime.Details.PreferredSearchTitle))
                    media = await FindAllMedia(anime, anime.Details.PreferredSearchTitle, episode);

                // If there is no preference toward either of them, time to set them
                else
                {

                    // If this is the very start of the series, we might also need to find episode start if the show
                    // is long running and indexing starts at another number (e.g. attack on titan s3 - 38 is episode 01)
                    if (anime.Episode == 0)
                    {
                        var english = await PotentialStartingEpisode(anime.Details.English) ?? (await FindAllMedia(anime, anime.Details.English, episode)).ToList();
                        var romanji = await PotentialStartingEpisode(anime.Details.Title) ?? (await FindAllMedia(anime, anime.Details.Title, episode)).ToList();
                        media = CalculateAndSetPreference(anime, english, romanji);
                        if (media.Count > 0 && !string.IsNullOrEmpty(anime.Details.PreferredSearchTitle))
                        {
                            anime.Episode = media.First().Episode - 1;

                            // if this is over the total, kill the total and have it be recalculated by the user later
                            if (anime.Episode > anime.Details.Total)
                            {
                                anime.Details.TotalEpisodes = 0;
                                anime.Details.OverallTotal = 0;
                            }
                        }
                    }
                    
                    else
                    {
                        var english = (await FindAllMedia(anime, anime.Details.English, episode)).ToList();
                        var romanji = (await FindAllMedia(anime, anime.Details.Title, episode)).ToList();
                        media = CalculateAndSetPreference(anime, english, romanji);
                    }

                    AnimeRepository.Save();
                }
            }

            else
                media = await FindAllMedia(anime, anime.Name, episode);

            return media;
        }

        // Absolutely generic

        public string ServiceName => GetType().Name.Replace("Service", "");

        public async Task<int> DownloadAll(IEnumerable<Anime> animes, Action<string> output)
        {
            var downloaded = 0;

            foreach (var anime in animes)
            {
                var result = await GetMedia(anime, anime.NextEpisode);
                var download = await AttemptDownload(anime, anime.NextEpisode, result, output);
                if (download)
                {
                    downloaded++;
                    anime.Episode++;
                    anime.Details.NeedsUpdating = true;
                }
            }

            if (downloaded > 0)
                AnimeRepository.Save();

            return downloaded;
        }

        public async Task<int> DownloadSpecificEpisodes(Dictionary<Anime, List<int>> animes, Action<string> output)
        {
            var downloaded = 0;

            foreach (var anime in animes.Keys)
            foreach (var episode in animes[anime])
            {
                var download = await AttemptDownload(anime, episode, await GetMedia(anime, episode), output);
                if (download)
                {
                    downloaded++;
                    anime.Details.NeedsUpdating = true;
                }
            }

            if (downloaded > 0)
                AnimeRepository.Save();

            return downloaded;
        }

        public bool CanDownload(RemoteMedia media, Anime anime)
        {
            if (anime == null || media == null)
                return false;

            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.ToLower().Equals(media.StrippedWithNoEpisode.ToLower()))
                return false;

            // Not the right subgroup
            if (!string.IsNullOrEmpty(anime.PreferredSubgroup))
            {
                if (!media.HasSubgroup())
                    return false;

                if (!media.Subgroup().ToLower().Contains(anime.PreferredSubgroup.ToLower()))
                    return false;
            }

            if (SettingsRepository.FlagConfig.OnlyWhitelisted)
            {
                // Torrent listing with no subgroup in the title
                if (!media.HasSubgroup())
                    return false;

                // Torrent listing with wrong subgroup
                if (!SettingsRepository.Subgroups.Select(s => s.ToLower()).Contains(media.Subgroup().ToLower()))
                    return false;
            }

            return true;
        }

        public async Task<bool> AttemptDownload(Anime anime, int episode, IEnumerable<RemoteMedia> medias, Action<string> output)
        {
            if (medias == null || anime == null)
                return false;

            foreach (var media in medias.Where(m => CanDownload(m, anime)))
                if (await DownloadEpisode(anime, episode, media, output))
                    return true;

            return false;
        }

        public async Task<bool> DownloadEpisode(Anime anime, int episode, RemoteMedia media, Action<string> output)
        {
            output($"Downloading '{anime.Title}' episode '{episode}'.");

            var download = await DownloadMedia(anime, media);

            if (download.successful)
            {
                StartMedia(media, download.command);
                await Log(anime, episode);
            }

            else
            {
                output($"Download of '{anime.Title}' failed (most likely due to server error).");
            }

            return download.successful;
        }

        public async Task<(bool successful, string command)> DownloadMedia(Anime anime, RemoteMedia media)
        {
            var path = "";
            bool successful;

            var destination = SettingsRepository.PathConfig.Unwatched;

            if (SettingsRepository.FlagConfig.IndividualShowFolders)
                destination = Path.Combine(destination, anime.Title);

            // Create directory
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            switch (media)
            {
                case Torrent torrent:
                {
                    (successful, path) = await DownloadTorrent(torrent);
                    if (!successful)
                        return (false, null);
                    break;
                }

                case MagnetLink magnet:
                {
                    (successful, path) = await Aria.Retrieve(magnet);
                    if (!successful)
                        return (false, null);
                    break;
                }
            }

            var command = SettingsRepository.TorrentDownloaderCommand(path, destination);

            return (true, command);
        }

        public virtual void StartMedia(RemoteMedia media, string command)
        {
            switch (media)
            {
                case Torrent _:
                case MagnetLink _:
                    StartInTorrentClient(command);
                    break;
            }
        }

        public async Task<bool> ServiceAvailable()
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(ServiceUrl);
                request.Timeout = 3000;
                request.Method = WebRequestMethods.Http.Head;
                request.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                request.Accept = "*/*";
                request.ContentLength = 0;
                request.Headers = new WebHeaderCollection {"cache-control: no-cache", "accept-encoding: gzip, deflate"};
                using (var response = await request.GetResponseAsync())
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected async Task Log(Anime anime, int episode)
        {
            var timestamp = $"{DateTime.Now:[M/d/yyyy @ hh:mm:ss tt]}";
            var message = $"Downloaded '{anime.Title}' episode {episode}.";
            using (var streamWriter = new StreamWriter(SettingsRepository.PathConfig.Logging, true))
            {
                await streamWriter.WriteLineAsync($"{timestamp} - {message}");
                streamWriter.Close();
            }
        }

        /// <summary>
        ///     Many sites have different ways their search works, but on the nyaa.* sites
        ///     this will transform the title into a searchable query.
        /// </summary>
        protected static string TransformEpisodeSearch(string name, int episode) => $"{TransformEpisodeSearch(name)}+{episode:D2}";

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
                .Replace("Souma", "Souma|Soma")       // ugly hack
                .Replace("'", "%27");

            return name;
        }
        
        public async Task<List<RemoteMedia>> FindAllMedia(Anime anime, int episode)
        {
            return await GetMedia(anime, episode);
        }

        // Abstract inheritors

        protected abstract ISettingsRepository SettingsRepository { get; }

        protected abstract IAnimeRepository AnimeRepository { get; }

        protected abstract IAnimeService AnimeService { get; }

        protected abstract WebClient Downloader { get; }

        public abstract string ServiceUrl { get; }

        public abstract Task<List<RemoteMedia>> FindAllMedia(Anime anime, string name, int episode);

        public abstract Task<List<RemoteMedia>> PotentialStartingEpisode(string name);

    }
}