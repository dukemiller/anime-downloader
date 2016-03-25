using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace anime_downloader.Classes {
    public class Downloader {
        private readonly Settings _settings;
        private readonly WebClient _client;

        public Downloader(Settings settings) {
            _settings = settings;
            _client = new WebClient();
        }

        /// <summary>
        ///     Attempt to download anime and display the results.
        /// </summary>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        /// <param name="textbox">The output box to display results to.</param>
        /// <param name="logger"></param>
        public async Task<int> DownloadAnime(IEnumerable<Anime> animes, TextBox textbox, Logger logger) {
            var downloaded = 0;

            textbox.Text = ">> Searching for currently airing anime episodes ...\n";

            foreach (var anime in animes) {

                var nyaaLinks = await anime.GetLinksToNextEpisode();

                if (nyaaLinks == null)
                    continue;

                foreach (var nyaa in nyaaLinks) {

                    if (nyaa == null)
                        continue;

                    // Most likely wrong torrent
                    if (anime.NameStrict && !anime.Name.Equals(nyaa.StrippedName(true)))
                        continue;

                    // Not the right subgroup
                    if (!anime.PreferredSubgroup.Equals("") & !nyaa.Subgroup().Contains(anime.PreferredSubgroup))
                        continue;

                    if (_settings.OnlyWhitelisted) {

                        // Nyaa listing with no subgroup in the title
                        if (!nyaa.HasSubgroup())
                            continue;

                        // Nyaa listing with wrong subgroup
                        if (!_settings.Subgroups.Contains(nyaa.Subgroup()))
                            continue;
                    }

                    textbox.AppendText($"Downloading '{anime.Title}' episode '{anime.NextEpisode()}'.\n");
                    textbox.ScrollDown();

                    await DownloadTorrent(nyaa);

                    if (logger.IsEnabled)
                        logger.WriteLine($"Downloaded '{anime.Title}' episode {anime.NextEpisode()}.");

                    anime.Episode = anime.NextEpisode();
                    downloaded++;
                    break;
                }
            }

            return downloaded;
        }

        private async Task DownloadTorrent(Nyaa nyaa) {
            if (_settings == null)
                return;

            var fileDirectory = _settings.GetOutputFolder();
            var filePath = Path.Combine(_settings.TorrentFilesPath, nyaa.TorrentName());

            if (!File.Exists(filePath))
                await _client.DownloadFileTaskAsync(nyaa.Link, filePath);

            if (!Directory.Exists(fileDirectory))
                Directory.CreateDirectory(fileDirectory);

            var command = $"/DIRECTORY \"{fileDirectory}\" \"{filePath}\"";
            CallCommand(_settings.UtorrentPath, command);
        }
        
        /// <summary>
        ///     Execute new process with given parameters.
        /// </summary>
        /// <param name="executable">Path to the executable file.</param>
        /// <param name="parameters">Arguments given to the executable.</param>
        private static void CallCommand(string executable, string parameters) {
            var info = new ProcessStartInfo {
                FileName = executable,
                Arguments = parameters,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = new Process {
                StartInfo = info
            };

            Task.Run(() => process.Start());
        }

    }
}