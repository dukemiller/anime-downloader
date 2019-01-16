using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight.Ioc;
using Optional;

namespace anime_downloader.Classes
{
    public static class MediaManager
    {
        private static ISettingsRepository SettingsRepository => SimpleIoc.Default.GetInstance<ISettingsRepository>();
        
        // 

        /// <summary>
        ///     Attempt to download the given {media} from an {anime}, returning the command to start the process.
        /// </summary>
        public static async Task<Option<string>> Download(Anime anime, RemoteMedia media)
        {
            var path = Option.None<string>();

            switch (media)
            {
                case Torrent torrent:
                    path = await WebFetch.Download(torrent);
                    break;
                case MagnetLink magnet:
                    path = await Aria.Download(magnet);
                    break;
            }

            // todo: this will only map to torrents and not xdcc/ftp/etc
            return path.Map(some => CreateUtorrentCommand(some, CreateDestination(anime)));
        }

        /// <summary>
        ///     Start the media correctly given the command.
        /// </summary>
        public static void Start(RemoteMedia media, string command)
        {
            switch (media)
            {
                case Torrent _:
                case MagnetLink _:
                    Start(command);
                    break;
            }
        }

        // 

        /// <summary>
        ///     Create the correct path for the file.
        /// </summary>
        private static string CreateDestination(Anime anime)
        {
            var destination = SettingsRepository.PathConfig.Unwatched;

            if (SettingsRepository.FlagConfig.IndividualShowFolders)
                destination = Path.Combine(destination, anime.Title);

            // Create directory
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            return destination;
        }

        /// <summary>
        ///     Get a command to add a new torrent to the torrent executable to download the file.
        /// </summary>
        /// <param name="torrent">The path to the .torrent file.</param>
        /// <param name="destination">The path to where the file should download to.</param>
        private static string CreateUtorrentCommand(string torrent, string destination)
        {
            var downloader = SettingsRepository.PathConfig.TorrentDownloader.ToLower();

            if (downloader.Contains("utorrent"))
                return $"/DIRECTORY \"{destination}\" \"{torrent}\"";

            // In the future on the latest qbittorent release, commandline path support will be enabled
            // and for now, all I can add is torrent files
            // https://github.com/qbittorrent/qBittorrent/issues/6979
            if (downloader.Contains("qbittorrent"))
                return $"{torrent}";
            // return $"--save-path=\"{destination}\" --add-paused=false --skip-dialog=true {torrent}";

            return $"{torrent}";
        }

        /// <summary>
        ///     Start the process and run the torrent given the user settings.
        /// </summary>
        private static void Start(string command)
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
    }

    internal static class WebFetch
    {
        private static ISettingsRepository SettingsRepository => SimpleIoc.Default.GetInstance<ISettingsRepository>();

        private static readonly WebClient Client = new WebClient();

        // 
        
        public static async Task<Option<string>> Download(Torrent torrent)
        {
            var torrentName = await torrent.GetTorrentNameAsync();
            if (torrentName == null)
                return Option.None<string>();
            var filePath = Path.Combine(SettingsRepository.PathConfig.Torrents, torrentName);

            // Download file 
            if (!File.Exists(filePath))
                return await Task.Run(() =>
                {
                    try
                    {
                        Client.DownloadFile(torrent.Remote, filePath);
                    }

                    // TODO: heh heh heh
                    catch (Exception)
                    {
                        return Option.None<string>();
                    }

                    return filePath.Some();
                });

            return filePath.Some();
        }
    }

    internal static class Aria
    {
        private const string ArchiveUrl = @"https://github.com/aria2/aria2/releases/download/release-1.31.0/aria2-1.31.0-win-32bit-build1.zip";

        private static string Directory => Path.Combine(PathConfiguration.ApplicationDirectory, "aria2");

        private static string Executable => Path.Combine(Directory, "aria2c.exe");

        private static ISettingsRepository SettingsRepository => SimpleIoc.Default.GetInstance<ISettingsRepository>();

        private static async Task DownloadAria()
        {
            var path = Path.Combine(PathConfiguration.ApplicationDirectory, "aria2.zip");

            using (var client = new WebClient())
                await client.DownloadFileTaskAsync(ArchiveUrl, path);

            ZipFile.ExtractToDirectory(path, PathConfiguration.ApplicationDirectory);

            File.Delete(path);

            System.IO.Directory.Move(
                Path.Combine(PathConfiguration.ApplicationDirectory, "aria2-1.31.0-win-32bit-build1"),
                Path.Combine(Directory));
        }

        // 

        /// <summary>
        ///     Retrieve the magnet link as a .torrent file, returning if successful and the path it was stored to.
        /// </summary>
        public static async Task<Option<string>> Download(MagnetLink magnet)
        {
            string file;

            if (!System.IO.Directory.Exists(Directory))
                await DownloadAria();

            var info = new ProcessStartInfo
            {
                FileName = Executable,
                Arguments = $"--bt-metadata-only=true --bt-save-metadata=true --bt-tracker={string.Join(",", magnet.Trackers)} {magnet.Hash}",
                WorkingDirectory = SettingsRepository.PathConfig.Torrents,
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

            if (result.Contains("Maybe file already exists"))
            {
                var torrent = result.Split('\n')
                                  .First(line => line.Contains(".torrent"))
                                  .Split('/')
                                  .Last()
                                  .Split('.')
                                  .First() + ".torrent";

                file = Path.Combine(SettingsRepository.PathConfig.Torrents, torrent);
            }

            else
            {
                file =
                    result.Split('\n')
                        .First(line => line.Contains("Saved metadata as"))
                        .Split(new[] { "Saved metadata as" }, StringSplitOptions.None)[1]
                        .TrimEnd('.')
                        .TrimStart(' ');
            }

            return file.Some();
        }
    }
}
