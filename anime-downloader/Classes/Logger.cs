using System;
using System.IO;
using System.Threading.Tasks;

namespace anime_downloader.Classes
{
    public class Logger
    {
        private readonly Settings _settings;

        public Logger(Settings settings)
        {
            _settings = settings;
        }

        public bool IsEnabled => _settings.UseLogging;

        public async Task WriteLine(string message)
        {
            var timestamp = $"{DateTime.Now:[yyyy/MM/d @ hh:mm:ss tt]}";

            using (var streamWriter = new StreamWriter(_settings.LogPath, true))
            {
                await streamWriter.WriteLineAsync($"{timestamp} - {message}");
                streamWriter.Close();
            }
        }
    }
}