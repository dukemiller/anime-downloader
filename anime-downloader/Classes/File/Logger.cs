using System;
using System.IO;
using System.Threading.Tasks;

namespace anime_downloader.Classes.File
{
    public class Logger
    {
        private readonly Settings _settings;

        public Logger(Settings settings)
        {
            _settings = settings;
        }

        public async Task WriteLineAsync(string message)
        {
            var timestamp = $"{DateTime.Now:[M/d/yyyy @ hh:mm:ss tt]}";

            using (var streamWriter = new StreamWriter(_settings.LoggingFile, true))
            {
                await streamWriter.WriteLineAsync($"{timestamp} - {message}");
                streamWriter.Close();
            }
        }
    }
}