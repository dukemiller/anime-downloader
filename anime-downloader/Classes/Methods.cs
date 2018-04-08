using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Models;
using anime_downloader.ViewModels.Dialogs;
using MaterialDesignThemes.Wpf;

namespace anime_downloader.Classes
{
    public static class Methods
    {
        /// <summary>
        ///     Compute the distance between two strings.
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];
            if (n == 0)
                return m;
            if (m == 0)
                return n;
            for (var i = 0; i <= n; d[i, 0] = i++)
            {
            }
            for (var j = 0; j <= m; d[0, j] = j++)
            {
            }
            for (var i = 1; i <= n; i++)
            for (var j = 1; j <= m; j++)
            {
                var cost = t[j - 1] == s[i - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
            return d[n, m];
        }

        /// <summary>
        ///     Strip the entire path of extraneous information (subgroups, resolution, etc).
        /// </summary>
        /// <param name="filename">
        ///     A file name, not a filepath.
        /// </param>
        /// <param name="removeEpisode">
        ///     A flag for also removing the episode number
        /// </param>
        public static string Strip(string filename, bool removeEpisode = false)
        {
            if (filename == null)
                return null;

            // Remove brackets and parenthesis
            filename = Regex.Replace(filename, @"\s?\[(.*?)\]|\((.*?)\)\s*", "");

            // Remove extensions
            filename = Regex.Replace(filename, @"\.mkv|\.mp4|\.avi", "");

            // Replace any "Episode 08" with just "08"
            var episode = Regex.Match(filename, @"[\-\s[\s_.]?(episode[\s_.]?(\d{1,3}))", RegexOptions.IgnoreCase);
            if (episode.Success)
                filename = filename.Replace(episode.Groups[1].Value, episode.Groups[2].Value);

            // Remove entirely anything saying the language sub
            filename = Regex.Replace(filename, @"-?[\s_.]?eng(?:lish|s)?[\s_.]?sub(?:bed|s)?", "", RegexOptions.IgnoreCase);

            // _ and . can be used for spaces for some subgroups, replace with spaces instead
            filename = new[] { "_", "." }.Aggregate(filename, (current, s) => current.Replace(s, " "));

            // Remove annoying fps counters
            filename = Regex.Replace(filename, @"\d{2,}\s?fps", "", RegexOptions.IgnoreCase);

            if (removeEpisode)
            {
                var regularEpisodePattern = Regex.Matches(filename, @"\-\s[0-9]{1,}"); // Name {- #}
                var namedEpisodePattern = Regex.Matches(filename, @"episode\s[0-9]{1,}", RegexOptions.IgnoreCase); // Name {Episode #}

                if (regularEpisodePattern.Count > 0)
                {
                    var split = filename.Split('-');
                    filename = string.Join("-", split.Take(split.Length - 1));
                }

                else if (namedEpisodePattern.Count > 0)
                {
                    var value =
                        namedEpisodePattern.Cast<Match>().Select(match => match.Groups[0].Value).ToList().First();

                    filename = filename.Replace(value, "");
                }
            }

            return Regex.Replace(filename.Trim(), @"\s+", " ");
        }

        /// <summary>
        ///     Display an alert message (currently a messagebox).
        /// </summary>
        public static async void Alert(string msg = "")
        {
            await DialogHost.Show(new MessageViewModel {Text = msg});
        }

        public static async Task<bool> QuestionYesNo(string question)
        {
            var result = true;
            await DialogHost.Show(new QuestionViewModel {Message = question}, (sender, args) =>
            {
                result = (bool) args.Parameter;
            });
            return result;
        }
        
        public static int Mod(int x, int m) => (x % m + m) % m;

        public static bool InRange(int number, int inclusiveBottom, int inclusiveTop) => number >= inclusiveBottom &&
                                                                                         number <= inclusiveTop;

        public static void MoveFile(AnimeFile file, string startPath, string movePath)
        {
            if (!File.Exists(file.Path))
                return;

            var relative = string.Join(Path.DirectorySeparatorChar.ToString(),
                file.Path.Split(Path.DirectorySeparatorChar)
                    .Skip(startPath.Split(Path.DirectorySeparatorChar).Length));
            var newPath = Path.Combine(movePath, relative);
            var fileDepth = relative.Split(Path.DirectorySeparatorChar);
            if (fileDepth.Length > 1)
            {
                var added = string.Join(Path.DirectorySeparatorChar.ToString(),
                    fileDepth.Take(fileDepth.Length - 1));
                Directory.CreateDirectory(Path.Combine(movePath, added));
            }
            Directory.Move(file.Path, newPath);
        }

    }
}