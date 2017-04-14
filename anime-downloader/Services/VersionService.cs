using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class VersionService : IVersionService
    {
        private readonly ISettingsService _settings;

        private const string Server = @"http://52.42.210.220";

        private readonly HttpClient _client;

        private readonly WebClient _downloader;

        private Task<SemanticVersion> _version;

        // 

        public VersionService(ISettingsService settings)
        {
            _settings = settings;
            _client = new HttpClient();
            _downloader = new WebClient();
            StartTimer();
        }

        // 

        public Task<SemanticVersion> OnlineVersion => _version ?? RefreshVersion();

        public SemanticVersion LocalVersion => new SemanticVersion(Assembly.GetExecutingAssembly().GetName().Version);

        // 

        public Task<SemanticVersion> RefreshVersion() => _version = RetrieveOnlineVersion();

        public async Task<bool> NeedsUpdate()
        {
            try
            {
                if (_settings.Version.NeedsUpdate)
                    return true;
                _settings.Version.NeedsUpdate = LocalVersion < await OnlineVersion;
                _settings.Save();
                return _settings.Version.NeedsUpdate;
            }
            catch
            {
                return false;
            }
        }

        // https://www.codeproject.com/articles/31454/how-to-make-your-application-delete-itself-immedia
        public async Task Update()
        {
            // Get URL for new exe
            var response = await _client.GetAsync($"{Server}/release");
            var release = await response.Content.ReadAsStringAsync();

            // Rename the current exe to filename + .bak (legal)
            var path = Assembly.GetEntryAssembly().Location;
            File.Move(path, path + ".bak");

            // Download the new file to the original name
            _downloader.DownloadFile(new Uri(release), path);

            // Set the version as up to date
            _settings.Version.NeedsUpdate = false;
            _settings.Save();

            // Close and start the application again
            Application.Current.Shutdown();
            Process.Start(path);

            // Delete the previous file
            var info = new ProcessStartInfo
            {
                Arguments = "/C choice /C Y /N /D Y /T 1 & Del " + path + ".bak",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };
            Process.Start(info);
        }

        private async Task<SemanticVersion> RetrieveOnlineVersion()
        {
            try
            {
                var response = await _client.GetAsync($"{Server}/version");
                var version = await response.Content.ReadAsStringAsync();
                return new SemanticVersion(version);
            }

            catch
            {
                return null;
            }
        }

        private void StartTimer()
        {
            var timer = new Timer
            {
                Interval = new TimeSpan(6, 0, 0).TotalMilliseconds,
                AutoReset = true,
                Enabled = true
            };

            timer.Elapsed += (sender, args) => RefreshVersion();
        }
    }
}