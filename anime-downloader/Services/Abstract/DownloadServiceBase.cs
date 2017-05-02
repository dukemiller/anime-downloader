using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

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
        // Absolutely generic

        public async Task<int> DownloadAll(IEnumerable<Anime> animes, Action<string> output)
        {
            var downloaded = 0;

            foreach (var anime in animes)
            {
                var download = await AttemptDownload(anime, await GetNextEpisode(anime), output);
                if (download)
                    downloaded++;
            }

            if (downloaded > 0)
                SettingsService.Save();

            return downloaded;
        }

        [NeedsUpdating]
        public async Task<int> DownloadAll(IEnumerable<Anime> animes, IEnumerable<AnimeFileRange> ranges,
            IEnumerable<AnimeFile> files, Action<string> output)
        {
            var downloaded = 0;

            foreach (var file in ranges)
            {
                var closest = AnimeService.ClosestAnime(file.Name);
                foreach (var episode in file.EpisodeRange)
                {
                    if (await Task.Run(() => files.Any(a => a.Name.Equals(file.Name) && a.Episode == episode)))
                        continue;

                    // TODO: make a copy constructor?
                    var anime = new Anime
                    {
                        Name = file.Name,
                        Episode = episode - 1,
                        Airing = closest.Airing,
                        Resolution = closest.Resolution,
                        PreferredSubgroup = closest.PreferredSubgroup,
                        NameStrict = closest.NameStrict
                    };

                    var download = await AttemptDownload(anime, await GetNextEpisode(anime), output);

                    if (download)
                        downloaded++;
                }
            }

            if (downloaded > 0)
                SettingsService.Save();

            return downloaded;
        }

        public bool CanDownload(Torrent torrent, Anime anime)
        {
            if (anime == null || torrent == null)
                return false;

            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.ToLower().Equals(torrent.StrippedWithNoEpisode.ToLower()))
                return false;

            // Not the right subgroup
            if (anime.PreferredSubgroup != null && torrent.Subgroup() != null)
                if (!string.IsNullOrEmpty(anime.PreferredSubgroup)
                    && !torrent.Subgroup().Contains(anime.PreferredSubgroup))
                    return false;

            if (SettingsService.FlagConfig.OnlyWhitelisted)
            {
                // Torrent listing with no subgroup in the title
                if (!torrent.HasSubgroup())
                    return false;

                // Torrent listing with wrong subgroup
                if (!SettingsService.Subgroups.Select(s => s.ToLower()).Contains(torrent.Subgroup().ToLower()))
                    return false;
            }

            return true;
        }

        public async Task<bool> AttemptDownload(Anime anime, IEnumerable<Torrent> torrents, Action<string> output)
        {
            if (torrents == null || anime == null)
                return false;

            foreach (var torrent in torrents.Where(torrent => CanDownload(torrent, anime)))
                if (await DownloadEpisode(anime, torrent, output))
                    return true;

            return false;
        }

        public async Task<bool> DownloadEpisode(Anime anime, Torrent torrent, Action<string> output)
        {
            output($"Downloading '{anime.Title}' episode '{anime.NextEpisode}'.");

            var download = await DownloadTorrent(anime, torrent);

            if (download.Successful)
            {
                StartTorrent(download.Command);
                anime.Episode++;
                await Log(anime);
            }

            else
            {
                output($"Download of '{anime.Title}' failed.");
            }

            return download.Successful;
        }

        public async Task<DownloadResult> DownloadTorrent(Anime anime, Torrent torrent)
        {
            var downloadResult = new DownloadResult {Successful = false};
            var torrentName = await torrent.GetTorrentNameAsync();
            if (torrentName == null)
                return downloadResult;

            var filePath = Path.Combine(SettingsService.PathConfig.Torrents, torrentName);
            var fileDirectory = SettingsService.PathConfig.Unwatched;

            if (SettingsService.FlagConfig.IndividualShowFolders)
                fileDirectory = Path.Combine(fileDirectory, anime.Title);

            var command = $"/DIRECTORY \"{fileDirectory}\" \"{filePath}\"";

            // Create directory
            if (!Directory.Exists(fileDirectory))
                Directory.CreateDirectory(fileDirectory);

            // Download file and call utorrent
            if (!File.Exists(filePath))
                return await Task.Run(() =>
                {
                    try
                    {
                        Downloader.DownloadFile(torrent.Link, filePath);
                    }

                    // TODO: heh heh heh
                    catch (Exception)
                    {
                        downloadResult.Successful = false;
                        return downloadResult;
                    }

                    downloadResult.Command = command;
                    downloadResult.Successful = true;
                    return downloadResult;
                });

            downloadResult.Command = command;
            downloadResult.Successful = true;
            return downloadResult;
        }

        protected void StartTorrent(string command)
        {
            var info = new ProcessStartInfo
            {
                FileName = SettingsService.PathConfig.TorrentDownloader,
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

        public async Task<bool> ServiceAvailable()
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(ServiceUrl);
                request.Timeout = 3000;
                request.AllowAutoRedirect = false;
                request.Method = "HEAD";

                using (var response = await request.GetResponseAsync())
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected async Task Log(Anime anime)
        {
            var timestamp = $"{DateTime.Now:[M/d/yyyy @ hh:mm:ss tt]}";
            var message = $"Downloaded '{anime.Title}' episode {anime.NextEpisode}.";
            using (var streamWriter = new StreamWriter(SettingsService.PathConfig.Logging, true))
            {
                await streamWriter.WriteLineAsync($"{timestamp} - {message}");
                streamWriter.Close();
            }
        }

        // Borders the line

        public async Task<IEnumerable<Torrent>> GetNextEpisode(Anime anime)
        {
            var result = await FindAllTorrents(anime, anime.NextEpisode);
            return result?
                .Select(torrent => new StringDistance<Torrent>(torrent, torrent.StrippedWithNoEpisode, anime.Name))
                .Where(ctd => ctd.Distance <= 25)
                .Select(ctd => ctd.Item);
        }

        // Abstract inheritors

        protected abstract ISettingsService SettingsService { get; }

        protected abstract IAnimeService AnimeService { get; }

        protected abstract WebClient Downloader { get; }

        public abstract string ServiceName { get; }

        public abstract string ServiceUrl { get; }

        public abstract Task<IEnumerable<Torrent>> FindAllTorrents(Anime anime, int episode);
    }
}