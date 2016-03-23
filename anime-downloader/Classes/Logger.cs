using System;
using System.IO;

namespace anime_downloader.Classes {
    public class Logger {

        private readonly Settings _settings;

        public Logger(Settings settings) {
            _settings = settings;
        }

        public bool IsEnabled { get; set; }

        public async void WriteLine(string message) {
            var timestamp = $"{DateTime.UtcNow:[yyyy:mm:dd][hh:mm:ss]}";

            using (var streamWriter = new StreamWriter(_settings.LogPath, true)) {
                await streamWriter.WriteLineAsync($"{timestamp} - {message}");
                streamWriter.Close();
            }
        }
        
    }
}
