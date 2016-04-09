using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using anime_downloader.Classes.Web;

namespace anime_downloader.Classes.File
{
    public class Downloader
    {
        private readonly Settings _settings;
        private readonly WebClient _client;
        private readonly Logger _logger;
        private int _downloaded;

        public Downloader(Settings settings)
        {
            _settings = settings;
            _logger = new Logger(settings);
            _client = new WebClient();
        }

        /// <summary>
        ///     Attempt to download anime and display the results.
        /// </summary>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        /// <param name="textbox">The output box to display results to.</param>
        public async Task<int> Download(List<Anime> animes, TextBox textbox)
        {
            _downloaded = 0;

            foreach (var anime in animes)
            {
                await DownloadEpisode(await anime.GetLinksToNextEpisode(), anime, textbox);
            }

            return _downloaded;
        }
        
        public async Task<int> Download(IEnumerable<Anime> animes,
            IEnumerable<AnimeEpisodeDelta> animeEpisodeDeltas,
            IEnumerable<AnimeEpisode> allEpisodes,
            TextBox textbox)
        {
            _downloaded = 0;
            var animeList = animes.ToList();

            foreach (var animeEpisode in animeEpisodeDeltas)
            {
                var animeBase = Anime.ClosestTo(animeList, animeEpisode.Name);
                foreach (var episode in animeEpisode.EpisodeRange)
                {
                    if (await Task.Run(() => allEpisodes.Any(a => a.Name.Equals(animeEpisode.Name) &&
                                                                  a.Episode.Equals(episode))))
                        continue;

                    // TODO: make a copy constructor?
                    var anime = new Anime
                    {
                        Name = animeEpisode.Name,
                        Episode = episode,
                        Airing = animeBase.Airing,
                        Resolution = animeBase.Resolution,
                        PreferredSubgroup = animeBase.PreferredSubgroup,
                        NameStrict = animeBase.NameStrict
                    };

                    await DownloadEpisode(await anime.GetLinksToNextEpisode(), anime, textbox);
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
                if (!anime.PreferredSubgroup.Equals("") && !torrent.Subgroup().Contains(anime.PreferredSubgroup))
                {
                    return false;
                }
            }

            if (_settings.OnlyWhitelisted)
            {
                // Nyaa listing with no subgroup in the title
                if (!torrent.HasSubgroup())
                    return false;

                // Nyaa listing with wrong subgroup
                if (!_settings.Subgroups.Contains(torrent.Subgroup()))
                    return false;
            }

            return true;
        }

        private async Task<bool> DownloadTorrent(TorrentProvider torrent, Anime anime, TextBox textbox)
        {
            textbox.WriteLine($"Downloading '{anime.Title}' episode '{anime.NextEpisode()}'.");
            if (await DownloadFile(torrent))
            {
                if (_logger.IsEnabled)
                    await _logger.WriteLine($"Downloaded '{anime.Title}' episode {anime.NextEpisode()}.");
                anime.Episode = anime.NextEpisode();
                _downloaded++;
                return true;
            }
            textbox.WriteLine($"Download of '{anime.Title} failed.");
            return false;
        }

        private async Task DownloadEpisode(IEnumerable<TorrentProvider> torrentLinks, Anime anime, TextBox textbox)
        {
            if (torrentLinks == null)
                return;

            foreach (var nyaa in torrentLinks.Where(nyaa => CanDownload(nyaa, anime)))
                if (await DownloadTorrent(nyaa, anime, textbox))
                    break;
        }

        private async Task<bool> DownloadFile(TorrentProvider nyaa)
        {

            var torrentName = nyaa.TorrentName();
            if (torrentName == null)
                return false;
            var filePath = Path.Combine(_settings.TorrentFilesPath, torrentName);
            var fileDirectory = _settings.GetEpisodeFolder();
            var command = $"/DIRECTORY \"{fileDirectory}\" \"{filePath}\"";

            _client.DownloadFileCompleted += delegate
            {
                CallCommand(_settings.UtorrentPath, command);
            };

            if (!Directory.Exists(fileDirectory))
                Directory.CreateDirectory(fileDirectory);

            if (!System.IO.File.Exists(filePath))
            {
                try
                {
                    await _client.DownloadFileTaskAsync(nyaa.Link, filePath);
                    return true;
                }

                // TODO: heh heh heh
                catch (Exception)
                {
                    // ignored
                    return false;
                }
                
            }

            CallCommand(_settings.UtorrentPath, command);
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