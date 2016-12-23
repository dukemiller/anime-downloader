using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Classes.Distances;
using anime_downloader.Classes.File;
using anime_downloader.Models;
using HtmlAgilityPack;

namespace anime_downloader.Services
{
    public class NyaaService : IAnimeDownloaderService
    {
        private const string EnglishTranslated = "1_37";

        private const string BySeeders = "2";

        private static readonly Dictionary<string, double> ToMegabyte = new Dictionary<string, double>
        {
            {"MiB", 1.04858},
            {"GiB", 1073.74},
            {"KiB", 0.001024}
        };

        private readonly WebClient _client;

        public NyaaService(ISettingsService settings, IAnimeFileService files)
        {
            Settings = settings;
            Files = files;
            _client = new WebClient();
        }

        private ISettingsService Settings { get; }

        public IAnimeFileService Files { get; set; }

        // 

        public bool CanDownload(Torrent torrent, Anime anime)
        {
            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.ToLower().Equals(torrent.StrippedWithNoEpisode.ToLower()))
                return false;

            // Not the right subgroup
            if ((anime.PreferredSubgroup != null) && (torrent.Subgroup() != null))
                if (!string.IsNullOrEmpty(anime.PreferredSubgroup) &&
                    !torrent.Subgroup().Contains(anime.PreferredSubgroup))
                    return false;

            if (Settings.FlagConfig.OnlyWhitelisted)
            {
                // Torrent listing with no subgroup in the title
                if (!torrent.HasSubgroup())
                    return false;

                // Torrent listing with wrong subgroup
                if (!Settings.Subgroups.Select(s => s.ToLower()).Contains(torrent.Subgroup().ToLower()))
                    return false;
            }

            return true;
        }

        public async Task<IEnumerable<Torrent>> GetNextEpisode(Anime anime)
        {
            var result = await GetTorrentsAsync(anime, anime.NextEpisode);
            return result?
                .Select(torrent => new StringDistance<Torrent>(torrent, torrent.StrippedWithNoEpisode, anime.Name))
                .Where(ctd => ctd.Distance <= 25)
                .Select(ctd => ctd.Item);
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
        
        public async Task<IEnumerable<Torrent>> GetTorrentsAsync(Anime anime, int episode)
        {
            var queryDetails = anime.Name.Replace(" ", "+")
                                   .Replace("'s", "")
                                   .Replace(".", "+")
                                   .Replace(":", " ")
                                   .Replace("!", "%21")
                                   .Replace("'", "%27")
                                   .Replace("-", "%2D")
                               + "+" + anime.Resolution + "+" + episode;

            var url = new Uri("https://www.nyaa.se/?page=rss" +
                              $"&cats={EnglishTranslated}" +
                              $"&term={queryDetails}" +
                              $"&sort={BySeeders}");

            var document = new HtmlDocument();

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadHtml(html);
            }

            var result = document.DocumentNode?
                .SelectNodes("//item")?
                .Select(CreateTorrent)
                .Where(n => n.Measurement.Equals("MiB")
                            && (n.Size > 5)
                            && n.StrippedName.Contains(episode.ToString("D2"))
                            && (n.Seeders > 0));

            if (anime.NameCollection.Any(c => c.Contains(episode.ToString("D2"))))
            {
                // To account for the case that a show contains a number (e.g. 12-sai - ep 12) that is 
                // relevant to the title and or also might contain the year in case of rework/reboot 
                // (e.g. Berserk (2016)) 
                const string fullYearPattern = @"\(\d{4}\)";
                result = result?
                    .Where(nyaa => nyaa.StrippedName.Split()
                                       .Select(c => Regex.Replace(c, fullYearPattern, ""))
                                       .Count(c => c.Contains(episode.ToString("D2")))
                                   >= 2);

                /* TODO
                 * This will fail in the case that there is a show with a name that contains the 
                 * same string multiple times in the name, something like "12 Dogfighter 12 - 12"
                */
            }

            return result?.OrderByDescending(n => n.Seeders);
        }

        public async Task<int> DownloadAsync(IEnumerable<Anime> animes, Action<string> output)
        {
            var downloaded = 0;
            
            foreach (var anime in animes)
            {
                var downloadSuccessful = await DownloadEpisodeAsync(await GetNextEpisode(anime), anime, output);
                if (downloadSuccessful)
                    downloaded++;
            }

            return downloaded;
        }

        [NeedsUpdating]
        public async Task<int> DownloadAsync(IEnumerable<Anime> animes, IEnumerable<AnimeFileRange> ranges, IEnumerable<AnimeFile> files, Action<string> output)
        {
            var downloaded = 0;

            var animeList = animes.ToList();

            foreach (var animeFile in ranges)
            {
                var animeBase = Files.ClosestAnime(animeList, animeFile.Name);
                foreach (var episode in animeFile.EpisodeRange)
                {
                    if (await Task.Run(() => files.Any(a => a.Name.Equals(animeFile.Name) && a.Episode == episode)))
                        continue;

                    // TODO: make a copy constructor?
                    var anime = new Anime
                    {
                        Name = animeFile.Name,
                        Episode = episode - 1,
                        Airing = animeBase.Airing,
                        Resolution = animeBase.Resolution,
                        PreferredSubgroup = animeBase.PreferredSubgroup,
                        NameStrict = animeBase.NameStrict
                    };

                    var downloadSuccessful =
                        await DownloadEpisodeAsync(await GetNextEpisode(anime), anime, output);

                    if (downloadSuccessful)
                        downloaded++;
                }
            }

            return downloaded;
        }

        public async Task<bool> DownloadEpisodeAsync(IEnumerable<Torrent> torrents, Anime anime, Action<string> output)
        {
            if ((torrents == null) || (anime == null))
                return false;

            foreach (var torrent in torrents.Where(torrent => CanDownload(torrent, anime)))
                if (await DownloadTorrentAsync(torrent, anime, output))
                    return true;

            return false;
        }

        public async Task<bool> DownloadTorrentAsync(Torrent torrent, Anime anime, Action<string> output)
        {
            output($"Downloading '{anime.Title}' episode '{anime.NextEpisode}'.");

            var fileWasDownloaded = await DownloadFileAsync(torrent, anime);

            if (fileWasDownloaded)
                anime.Episode = anime.NextEpisode;

            else
                output($"Download of '{anime.Title}' failed.");

            return fileWasDownloaded;
        }

        public async Task<bool> DownloadFileAsync(Torrent torrent, Anime anime)
        {
            var torrentName = await torrent.GetTorrentNameAsync();
            if (torrentName == null)
                return false;

            var filePath = Path.Combine(Settings.PathConfig.Torrents, torrentName);
            var fileDirectory = Settings.PathConfig.Unwatched;

            if (Settings.FlagConfig.IndividualShowFolders)
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
                        // ignored
                        return false;
                    }

                    CallCommand(Settings.PathConfig.TorrentDownloader, command);
                    return true;
                });

            CallCommand(Settings.PathConfig.TorrentDownloader, command);
            return true;
        }

        private static Torrent CreateTorrent(HtmlNode node)
        {
            var torrent = new Torrent
            {
                Name = WebUtility.HtmlDecode(node.Element("title").InnerText.Replace("Â", "")),
                Link = node.Element("#text").InnerText.Replace("#38;", "")
            };

            var description = node.Element("description").InnerText;
            if (description.Contains("CDATA"))
                description = description
                    .Split(new[] { "<![CDATA[" }, StringSplitOptions.None)[1]
                    .Split(new[] { "]]>" }, StringSplitOptions.None)[0];
            torrent.Description = description;
            torrent.Seeders = int.Parse(description.Split(new[] { " seeder" }, StringSplitOptions.None)[0]);
            torrent.Measurement = ToMegabyte.First(d => description.Contains(d.Key)).Key;
            torrent.Size = Math.Round(double.Parse(description.Split(new[] { $" {torrent.Measurement}" }, StringSplitOptions.None)[0]
                .Split(new[] { " - " }, StringSplitOptions.None)[1]) * ToMegabyte[torrent.Measurement], 2);

            return torrent;
        }

        // 

        /// <summary>
        ///     Execute new process with given parameters.
        /// </summary>
        /// <param name="executable">Path to the executable file.</param>
        /// <param name="parameters">Arguments given to the executable.</param>
        private static void CallCommand(string executable, string parameters)
        {
            var info = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = parameters,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = new Process
            {
                StartInfo = info
            };

            Task.Run(() => process.Start());
        }
    }
}