using System;
using System.IO;
using System.Threading.Tasks;
using anime_downloader.Models;

namespace anime_downloader.Classes.File
{
    public static class Logger
    {
        public static async Task WriteLineAsync(string message)
        {
            var timestamp = $"{DateTime.Now:[M/d/yyyy @ hh:mm:ss tt]}";

            using (var streamWriter = new StreamWriter(Settings.LoggingFile, true))
            {
                await streamWriter.WriteLineAsync($"{timestamp} - {message}");
                streamWriter.Close();
            }
        }
    }
}