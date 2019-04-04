using System;
using System.Globalization;
using Newtonsoft.Json;

namespace anime_downloader
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <summary>
        ///     Used for creating proper capitalized titles.
        /// </summary>
        public static readonly TextInfo TextInfo = new CultureInfo("en-US", false).TextInfo;

        /// <summary>
        ///     Filepaths used in the application.
        /// </summary>
        public static class Path
        {
            public static class Directory
            {
                /// <summary>
                ///     The path to the folder containing all settings and configuration files.
                /// </summary>
                public static string Application =>
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "anime_downloader");

                
                public static string Duplicates =>
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

                /// <summary>
                ///     Path to the current working directory (relative to launched exe)
                /// </summary>
                public static string CurrentWorking => 
                    System.IO.Directory.GetCurrentDirectory();

                /// <summary>
                ///     Path to the images directory.
                /// </summary>
                public static string Images => 
                    System.IO.Path.Combine(Application, "images");
            }

            /// <summary>
            ///     The path to the playlist file.
            /// </summary>
            public static string Playlist => System.IO.Path.Combine(Directory.Application, "playlist.m3u");

            /// <summary>
            ///     The path to the episodes playlist file.
            /// </summary>
            public static string Episodes => System.IO.Path.Combine(Directory.Application, "episodes.m3u");

            /// <summary>
            ///     The path to the log text file.
            /// </summary>
            public static string Logging => System.IO.Path.Combine(Directory.Application, "log.txt");

            /// <summary>
            ///     Path to the saved settings file.
            /// </summary>
            public static string Settings => System.IO.Path.Combine(Directory.Application, "settings.json");
        }
    }
}