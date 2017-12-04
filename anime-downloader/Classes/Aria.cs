using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Repositories.Interface;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.Classes
{
    public static class Aria
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
        public static async Task<(bool successful, string path)> Retrieve(MagnetLink magnet)
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

            return (true, file);
        }
    }
}