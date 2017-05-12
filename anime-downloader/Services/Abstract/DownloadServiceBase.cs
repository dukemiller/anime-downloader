using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Models.Configurations;
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
        // 

        /// <summary>
        ///     The max age (in days) a torrent can be for it to still be in this season
        /// </summary>
        protected static int MaxAge => (DateTime.Now - DateTime.Parse($"{((int)CurrentSeason() - 1) * 3 + 1}/1")).Days;

        private static Season CurrentSeason() => (Season)Math.Ceiling(Convert.ToDouble(DateTime.Now.Month) / 3);

        private static string AriaDirectory => Path.Combine(PathConfiguration.ApplicationDirectory, "aria2");

        private static string AriaExecutable => Path.Combine(AriaDirectory, "aria2c.exe");

        private async Task DownloadAria()
        {
            const string url =
                @"https://github.com/aria2/aria2/releases/download/release-1.31.0/aria2-1.31.0-win-32bit-build1.zip";
            var path = Path.Combine(PathConfiguration.ApplicationDirectory, "aria2.zip");
            await Downloader.DownloadFileTaskAsync(url, path);
            System.IO.Compression.ZipFile.ExtractToDirectory(path, PathConfiguration.ApplicationDirectory);
            File.Delete(path);
            Directory.Move(
                Path.Combine(PathConfiguration.ApplicationDirectory, "aria2-1.31.0-win-32bit-build1"),
                Path.Combine(AriaDirectory));
        }

        private async Task<(bool successful, string path)> RetrieveFromAria(MagnetLink magnet)
        {
            if (!Directory.Exists(AriaDirectory))
                await DownloadAria();

            var info = new ProcessStartInfo
            {
                FileName = AriaExecutable,
                Arguments = $"--bt-metadata-only=true --bt-save-metadata=true --bt-tracker={string.Join(",", magnet.Trackers)} {magnet.Hash}",
                WorkingDirectory = SettingsService.PathConfig.Torrents,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var process = new Process
            {
                StartInfo = info
            };

            process.Start();

            var result = await process.StandardOutput.ReadToEndAsync();

            var file =
                result.Split('\n')
                    .First(line => line.Contains("Saved metadata as"))
                    .Split(new[] {"Saved metadata as"}, StringSplitOptions.None)[1]
                    .Split(Path.PathSeparator)
                    .Last()
                    .TrimEnd('.')
                    .TrimStart(' ');

            return (true, file);
        }

        private async Task<(bool successful, string path)> DownloadTorrent(Torrent torrent)
        {
            var torrentName = await torrent.GetTorrentNameAsync();
            if (torrentName == null)
                return (false, null);
            var filePath = Path.Combine(SettingsService.PathConfig.Torrents, torrentName);

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

        // Absolutely generic

        public string ServiceName => GetType().Name.Replace("Service", "");

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

        public bool CanDownload(RemoteMedia media, Anime anime)
        {
            if (anime == null || media == null)
                return false;

            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.ToLower().Equals(media.StrippedWithNoEpisode.ToLower()))
                return false;

            // Not the right subgroup
            if (anime.PreferredSubgroup != null && media.Subgroup() != null)
                if (!string.IsNullOrEmpty(anime.PreferredSubgroup)
                    && !media.Subgroup().Contains(anime.PreferredSubgroup))
                    return false;

            if (SettingsService.FlagConfig.OnlyWhitelisted)
            {
                // Torrent listing with no subgroup in the title
                if (!media.HasSubgroup())
                    return false;

                // Torrent listing with wrong subgroup
                if (!SettingsService.Subgroups.Select(s => s.ToLower()).Contains(media.Subgroup().ToLower()))
                    return false;
            }

            return true;
        }

        public async Task<bool> AttemptDownload(Anime anime, IEnumerable<RemoteMedia> medias, Action<string> output)
        {
            if (medias == null || anime == null)
                return false;

            foreach (var media in medias.Where(m => CanDownload(m, anime)))
                if (await DownloadEpisode(anime, media, output))
                    return true;

            return false;
        }

        public async Task<bool> DownloadEpisode(Anime anime, RemoteMedia media, Action<string> output)
        {
            output($"Downloading '{anime.Title}' episode '{anime.NextEpisode}'.");

            var download = await DownloadMedia(anime, media);

            if (download.successful)
            {
                StartMedia(media, download.command);
                anime.Episode++;
                await Log(anime);
            }

            else
            {
                output($"Download of '{anime.Title}' failed.");
            }

            return download.successful;
        }

        public async Task<(bool successful, string command)> DownloadMedia(Anime anime, RemoteMedia media)
        {
            var path = "";
            bool successful;

            var fileDirectory = SettingsService.PathConfig.Unwatched;
            if (SettingsService.FlagConfig.IndividualShowFolders)
                fileDirectory = Path.Combine(fileDirectory, anime.Title);

            // Create directory
            if (!Directory.Exists(fileDirectory))
                Directory.CreateDirectory(fileDirectory);

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
                    (successful, path) = await RetrieveFromAria(magnet);
                    if (!successful)
                        return (false, null);
                    break;
                }
            }

            var command = $"/DIRECTORY \"{fileDirectory}\" \"{path}\"";

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

        public async Task<IEnumerable<RemoteMedia>> GetNextEpisode(Anime anime)
        {
            var result = await FindAllMedia(anime, anime.NextEpisode);
            return result?
                .Select(torrent => new StringDistance<RemoteMedia>(torrent, torrent.StrippedWithNoEpisode, anime.Name))
                .Where(ctd => ctd.Distance <= 25)
                .Select(ctd => ctd.Item);
        }

        // Abstract inheritors

        protected abstract ISettingsService SettingsService { get; }

        protected abstract IAnimeService AnimeService { get; }

        protected abstract WebClient Downloader { get; }

        public abstract string ServiceUrl { get; }

        public abstract Task<IEnumerable<RemoteMedia>> FindAllMedia(Anime anime, int episode);
    }
}