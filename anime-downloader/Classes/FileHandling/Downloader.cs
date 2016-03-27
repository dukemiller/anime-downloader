using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using anime_downloader.Classes.Web;

namespace anime_downloader.Classes.FileHandling {
    public class Downloader {
        private readonly Settings _settings;
        private readonly WebClient _client;

        public Downloader(Settings settings) {
            _settings = settings;
            _client = new WebClient();
        }

        public bool CanDownload(Nyaa nyaa, Anime anime) {
            if (nyaa == null)
                return false; 

            // Most likely wrong torrent
            if (anime.NameStrict && !anime.Name.Equals(nyaa.StrippedName(true)))
                return false;

            // Not the right subgroup
            if (!anime.PreferredSubgroup.Equals("") & !nyaa.Subgroup().Contains(anime.PreferredSubgroup))
                return false; 

            if (_settings.OnlyWhitelisted) {

                // Nyaa listing with no subgroup in the title
                if (!nyaa.HasSubgroup())
                    return false;

                // Nyaa listing with wrong subgroup
                if (!_settings.Subgroups.Contains(nyaa.Subgroup()))
                    return false; 
            }

            return true;
        }

        /// <summary>
        ///     Attempt to download anime and display the results.
        /// </summary>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        /// <param name="textbox">The output box to display results to.</param>
        /// <param name="logger"></param>
        public async Task<int> DownloadAnime(IEnumerable<Anime> animes, TextBox textbox, Logger logger) {
            var downloaded = 0;

            foreach (var anime in animes) {

                var nyaaLinks = await anime.GetLinksToNextEpisode();

                if (nyaaLinks == null)
                    continue;

                foreach (var nyaa in nyaaLinks.Where(nyaa => CanDownload(nyaa, anime))) {
                    textbox.AppendText($"Downloading '{anime.Title}' episode '{anime.NextEpisode()}'.\n");
                    textbox.ScrollDown();
                    await DownloadTorrent(nyaa);
                    if (logger.IsEnabled)
                        await logger.WriteLine($"Downloaded '{anime.Title}' episode {anime.NextEpisode()}.");
                    anime.Episode = anime.NextEpisode();
                    downloaded++;
                    break;
                }
            }

            return downloaded;
        }

        public async Task DownloadTorrent(Nyaa nyaa) {
            if (_settings == null)
                return;

            var fileDirectory = _settings.GetEpisodeFolder();
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
        public static void CallCommand(string executable, string parameters) {
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