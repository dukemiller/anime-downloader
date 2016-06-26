using anime_downloader.Classes.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace anime_downloader.Classes.File
{
    public class Downloader
    {
        private readonly WebClient _client;

        private readonly Settings _settings;

        private int _downloaded;

        public Downloader(Settings settings)
        {
            _settings = settings;
            _client = new WebClient();
        }

        /// <summary>
        ///     Attempt to download anime and display the results.
        /// </summary>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        /// <param name="textbox">The output box to display results to.</param>
        public async Task<int> DownloadAsync(List<Anime> animes, TextBox textbox)
        {
            _downloaded = 0;

            foreach (var anime in animes)
                await DownloadEpisodeAsync(await anime.GetLinksToNextEpisode(), anime, textbox);

            return _downloaded;
        }

        public async Task<int> DownloadAsync(IEnumerable<Anime> animes,
            IEnumerable<AnimeEpisodeDelta> animeEpisodeDeltas,
            IEnumerable<AnimeEpisode> allEpisodes,
            TextBox textbox)
        {
            _downloaded = 0;
            var animeList = animes.ToList();

            foreach (var animeEpisode in animeEpisodeDeltas)
            {
                var animeBase = Anime.Closest.To(animeEpisode.Name, animeList);
                foreach (var episode in animeEpisode.EpisodeRange)
                {
                    if (await Task.Run(() => allEpisodes.Any(a => a.Name.Equals(animeEpisode.Name) && a.Episode.Equals(episode))))
                        continue;

                    var previousEpisode = $"{int.Parse(episode) - 1:D2}";

                    // TODO: make a copy constructor?
                    var anime = new Anime
                    {
                        Name = animeEpisode.Name,
                        Episode = previousEpisode,
                        Airing = animeBase.Airing,
                        Resolution = animeBase.Resolution,
                        PreferredSubgroup = animeBase.PreferredSubgroup,
                        NameStrict = animeBase.NameStrict
                    };

                    await DownloadEpisodeAsync(await anime.GetLinksToNextEpisode(), anime, textbox);
                }
            }

            return _downloaded;
        }

        public bool CanDownload(TorrentProvider torrent, Anime anime)
        {
            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.ToLower().Equals(torrent.StrippedName(true).ToLower()))
                return false;

            // Not the right subgroup
            if (anime.PreferredSubgroup != null && torrent.Subgroup() != null)
            {
                if (!anime.PreferredSubgroup.IsBlank() && !torrent.Subgroup().Contains(anime.PreferredSubgroup))
                {
                    return false;
                }
            }

            if (_settings.Flags.OnlyWhitelisted)
            {
                // Torrent listing with no subgroup in the title
                if (!torrent.HasSubgroup())
                    return false;

                // Torrent listing with wrong subgroup
                if (!_settings.Subgroups.Select(s => s.ToLower()).Contains(torrent.Subgroup().ToLower()))
                    return false;
            }

            return true;
        }

        public async Task<bool> DownloadEpisodeAsync(IEnumerable<TorrentProvider> torrentLinks, Anime anime,
            TextBox textbox)
        {
            if (torrentLinks == null || anime == null)
                return false;

            foreach (var torrent in torrentLinks.Where(torrent => CanDownload(torrent, anime)))
                if (await DownloadTorrentAsync(torrent, anime, textbox))
                    return true;

            return false;
        }

        public async Task<bool> DownloadTorrentAsync(TorrentProvider torrent, Anime anime, TextBox textbox)
        {
            textbox.WriteLine($"Downloading '{anime.Title}' episode '{anime.NextEpisode()}'.");
            var fileWasDownloaded = await DownloadFileAsync(torrent, anime);

            if (fileWasDownloaded)
            {
                await Logger.WriteLineAsync($"Downloaded '{anime.Title}' episode {anime.NextEpisode()}.");
                anime.Episode = anime.NextEpisode();
                _downloaded++;
            }
            else
            {
                textbox.WriteLine($"Download of '{anime.Title}' failed.");
            }

            return fileWasDownloaded;
        }

        public async Task<bool> DownloadFileAsync(TorrentProvider torrent, Anime anime)
        {
            var torrentName = await torrent.GetTorrentNameAsync();
            if (torrentName == null)
                return false;
            var filePath = Path.Combine(_settings.Paths.TorrentFilesDirectory, torrentName);
            var fileDirectory = _settings.Paths.EpisodeDirectory;

            if (_settings.Flags.IndividualShowFolders)
                fileDirectory = Path.Combine(fileDirectory, anime.Title);

            var command = $"/DIRECTORY \"{fileDirectory}\" \"{filePath}\"";

            // Create directory
            if (!Directory.Exists(fileDirectory))
                Directory.CreateDirectory(fileDirectory);

            // Download file and call utorrent
            if (!System.IO.File.Exists(filePath))
            {
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

                    CallCommand(_settings.Paths.UtorrentFile, command);
                    return true;
                });
            }

            CallCommand(_settings.Paths.UtorrentFile, command);
            return true;
        }

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