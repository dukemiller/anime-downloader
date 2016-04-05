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
        private readonly WebClient _client;
        private readonly Logger _logger;
        private readonly Settings _settings;

        public Downloader(Settings settings)
        {
            _settings = settings;
            _logger = new Logger(settings);
            _client = new WebClient();
        }

        private bool CanDownload(Nyaa nyaa, Anime anime)
        {
            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.Equals(nyaa.StrippedName(true)))
                return false;

            // Not the right subgroup
            if (anime.PreferredSubgroup != null && (!anime.PreferredSubgroup?.Equals("") & !nyaa.Subgroup()?.Contains(anime.PreferredSubgroup) ?? true))
                return false;

            if (_settings.OnlyWhitelisted)
            {
                // Nyaa listing with no subgroup in the title
                if (!nyaa.HasSubgroup())
                    return false;

                // Nyaa listing with wrong subgroup
                if (!_settings.Subgroups?.Contains(nyaa.Subgroup()) ?? true)
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Attempt to download anime and display the results.
        /// </summary>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        /// <param name="textbox">The output box to display results to.</param>
        public async Task<int> Download(IEnumerable<Anime> animes, TextBox textbox)
        {
            var downloaded = 0;

            foreach (var anime in animes)
            {
                var nyaaLinks = await anime.GetLinksToNextEpisode();

                if (nyaaLinks == null)
                    continue;

                foreach (var nyaa in nyaaLinks.Where(nyaa => nyaa != null && CanDownload(nyaa, anime)))
                {
                    textbox.AppendText($"Downloading '{anime.Title}' episode '{anime.NextEpisode()}'.\n");
                    textbox.ScrollDown();
                    if (await DownloadFile(nyaa))
                    {
                        if (_logger.IsEnabled)
                            await _logger.WriteLine($"Downloaded '{anime.Title}' episode {anime.NextEpisode()}.");
                        anime.Episode = anime.NextEpisode();
                        downloaded++;
                        break;
                    }
                    else
                    {
                        textbox.AppendText($"Download of '{anime.Title} failed.\n");
                    }
                }
            }

            return downloaded;
        }

        public async Task<int> Download(IEnumerable<Anime> animes,
            IEnumerable<AnimeEpisodeDelta> animeEpisodeDeltas,
            IEnumerable<AnimeEpisode> allEpisodes)
        {
            var downloaded = 0;
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

                    var nyaaLinks = await anime.GetLinksToCurrentEpisode();

                    foreach (var nyaa in nyaaLinks.Where(nyaa => CanDownload(nyaa, anime)))
                    {
                        //_downloadQueue.Enqueue(() => StartDownload(nyaa));
                        var downloadSuccessful = await DownloadFile(nyaa);
                        if (downloadSuccessful)
                        {
                            if (_logger.IsEnabled)
                                await _logger.WriteLine($"Downloaded '{anime.Title}' episode {anime.NextEpisode()}.");
                            downloaded++;
                            break;
                        }
                        else
                        {
                            
                        }
                    }
                }
            }
            return downloaded;
        }

        private async Task<bool> DownloadFile(Nyaa nyaa)
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
                await _client.DownloadFileTaskAsync(nyaa.Link, filePath);
                return true;
            }

            else
            {
                CallCommand(_settings.UtorrentPath, command);
                return true;
            }
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

    internal class DownloadQueue
    {
        private readonly ConcurrentQueue<Action> _queue;
        private Action _result;

        public DownloadQueue()
        {
            _queue = new ConcurrentQueue<Action>();
        }

        public async void Enqueue(Action a)
        {
            _queue.Enqueue(a);
            while (_queue.TryDequeue(out _result))
                await Task.Run(_result);
        }
    }
}