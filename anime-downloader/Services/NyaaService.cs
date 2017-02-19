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
using anime_downloader.Services.Interfaces;
using HtmlAgilityPack;

namespace anime_downloader.Services
{
    public class NyaaService : IDownloadService
    {
        private const string YearPattern = @"[^a-zA-Z](\d{4})[^a-zA-Z]";

        private const string EnglishTranslated = "1_37";

        private const string BySeeders = "2";

        private readonly IAnimeService _animeService;

        private readonly ISettingsService _settingsService;

        private readonly WebClient _client;

        // 

        public NyaaService(ISettingsService settingsService, IAnimeService animeService)
        {
            _settingsService = settingsService;
            _animeService = animeService;
            _client = new WebClient();
        }

        public string ServiceName => "Nyaa";

        public bool CanDownload(Torrent torrent, Anime anime)
        {
            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.ToLower().Equals(torrent.StrippedWithNoEpisode.ToLower()))
                return false;

            // Not the right subgroup
            if (anime.PreferredSubgroup != null && torrent.Subgroup() != null)
                if (!string.IsNullOrEmpty(anime.PreferredSubgroup) &&
                    !torrent.Subgroup().Contains(anime.PreferredSubgroup))
                    return false;

            if (_settingsService.FlagConfig.OnlyWhitelisted)
            {
                // Torrent listing with no subgroup in the title
                if (!torrent.HasSubgroup())
                    return false;

                // Torrent listing with wrong subgroup
                if (!_settingsService.Subgroups.Select(s => s.ToLower()).Contains(torrent.Subgroup().ToLower()))
                    return false;
            }

            return true;
        }

        public async Task<bool> ServiceAvailable()
        {
            try
            {
                const string link = "https://www.nyaa.se/";
                var request = (HttpWebRequest) WebRequest.Create(link);
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

        // 

        public async Task<IEnumerable<Torrent>> FindAllTorrents(Anime anime, int episode)
        {
            var document = new HtmlDocument();

            var url = new Uri("https://www.nyaa.se/?page=rss" +
                              $"&cats={EnglishTranslated}" +
                              $"&term={NyaaTerms(anime, episode)}" +
                              $"&sort={BySeeders}");

            Console.WriteLine(url);

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadHtml(html);
            }

            return ParseResults(anime, episode, document);
        }

        public async Task<IEnumerable<Torrent>> GetNextEpisode(Anime anime)
        {
            var result = await FindAllTorrents(anime, anime.NextEpisode);
            return result?
                .Select(torrent => new StringDistance<Torrent>(torrent, torrent.StrippedWithNoEpisode, anime.Name))
                .Where(ctd => ctd.Distance <= 25)
                .Select(ctd => ctd.Item);
        }

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
                _settingsService.Save();

            return downloaded;
        }

        [NeedsUpdating]
        public async Task<int> DownloadAll(
            IEnumerable<Anime> animes,
            IEnumerable<AnimeFileRange> ranges,
            IEnumerable<AnimeFile> files,
            Action<string> output)
        {
            var downloaded = 0;

            foreach (var file in ranges)
            {
                var closest = _animeService.ClosestAnime(file.Name);
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
                _settingsService.Save();

            return downloaded;
        }

        public async Task<bool> AttemptDownload(Anime anime, IEnumerable<Torrent> torrents, Action<string> output)
        {
            if (torrents == null || anime == null)
                return false;

            foreach (var torrent in torrents.Where(torrent => CanDownload(torrent, anime)))
            {
                if (await DownloadEpisode(anime, torrent, output))
                    return true;
            }

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

            var filePath = Path.Combine(_settingsService.PathConfig.Torrents, torrentName);
            var fileDirectory = _settingsService.PathConfig.Unwatched;

            if (_settingsService.FlagConfig.IndividualShowFolders)
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
                        _client.DownloadFile(torrent.Link, filePath);
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

        private void StartTorrent(string command)
        {
            var info = new ProcessStartInfo
            {
                FileName = _settingsService.PathConfig.TorrentDownloader,
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

        // Query functions

        private static string NyaaTerms(Anime anime, int episode)
        {
            var name = anime.Name
                .Replace(" ", "+").Replace("'s", "").Replace(".", "+").Replace(":", " ")
                .Replace("!", "%21").Replace("'", "%27").Replace("-", "%2D");
            return $"{name}+{episode:D2}";
        }

        private static IEnumerable<Torrent> ParseResults(Anime anime, int episode, HtmlDocument document)
        {
            var result = document.DocumentNode?
                .SelectNodes("//item")?
                .Select(node => node.ToTorrent())
                .Where(n => n.Measurement.Equals("MiB")
                            && n.Size > 5
                            && Regex.Split(n.StrippedName, " ").Any(s => s.Contains(episode.ToString("D2")))
                            && !n.Name.ToLower().Contains("(movie)")
                            && !n.Name.ToLower()
                                .Replace("-", "")
                                .Replace("_", "")
                                .Replace(" ", "")
                                .Contains($"part{episode:D2}")
                );

            if (anime.NameCollection.Any(c => Regex.Replace(c, YearPattern, "").Contains(episode.ToString("D2"))))
            {
                // To account for the case that a show contains a number (e.g. 12-sai - ep 12) that is 
                // relevant to the title and or also might contain the year in case of rework/reboot 
                // (e.g. Berserk (2016)) 
                result = result?
                    .Where(nyaa => nyaa.StrippedName.Split()
                                       .Select(c => Regex.Replace(c, YearPattern, ""))
                                       .Count(c => c.Contains(episode.ToString("D2")))
                                   >= 2);

                /* TODO
                 * This will fail in the case that there is a show with a name that contains the 
                 * same string multiple times in the name, something like "12 Dogfighter 12 - 12"
                */
            }

            return result?.OrderByDescending(n => n.Name.Contains(anime.Resolution)).ThenByDescending(n => n.Seeders);
        }

        private async Task Log(Anime anime)
        {
            var timestamp = $"{DateTime.Now:[M/d/yyyy @ hh:mm:ss tt]}";
            var message = $"Downloaded '{anime.Title}' episode {anime.NextEpisode}.";
            using (var streamWriter = new StreamWriter(_settingsService.PathConfig.Logging, true))
            {
                await streamWriter.WriteLineAsync($"{timestamp} - {message}");
                streamWriter.Close();
            }
        }
    }

    internal static class NyaaExtension
    {
        private static readonly Dictionary<string, double> ToMegabyte = new Dictionary<string, double>
        {
            {"MiB", 1.04858},
            {"GiB", 1073.74},
            {"KiB", 0.001024}
        };

        public static Torrent ToTorrent(this HtmlNode node)
        {
            var torrent = new Torrent
            {
                Name = WebUtility.HtmlDecode(node.Element("title").InnerText.Replace("Â", "")),
                Link = node.Element("#text").InnerText.Replace("#38;", "")
            };

            var description = node.Element("description").InnerText;
            if (description.Contains("CDATA"))
                description = description
                    .Split(new[] {"<![CDATA["}, StringSplitOptions.None)[1]
                    .Split(new[] {"]]>"}, StringSplitOptions.None)[0];
            torrent.Description = description;
            torrent.Seeders = int.Parse(description.Split(new[] {" seeder"}, StringSplitOptions.None)[0]);
            torrent.Measurement = ToMegabyte.First(d => description.Contains(d.Key)).Key;
            torrent.Size =
                Math.Round(double.Parse(description.Split(new[] {$" {torrent.Measurement}"}, StringSplitOptions.None)[0]
                               .Split(new[] {" - "}, StringSplitOptions.None)[1]) * ToMegabyte[torrent.Measurement], 2);
            return torrent;
        }
    }
}