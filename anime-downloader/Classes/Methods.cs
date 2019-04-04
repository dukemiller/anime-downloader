using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.ViewModels.Dialogs;
using MaterialDesignThemes.Wpf;
using static System.String;

namespace anime_downloader.Classes
{
    public static class Methods
    {
        /// <summary>
        ///     Given any amount of predicates, return if any value is true.
        /// </summary>
        public static Func<T, bool> Or<T>(params Func<T, bool>[] predicates) =>
            args => predicates.Any(predicate => predicate(args));

        /// <summary>
        ///     Flatten items and collections of items into a single List&lt;T&gt; of given args that are type &lt;T&gt;,
        ///     e.g. Flatten&lt;int&gt;(1, 2, "3", false, new[]{ 4, 5 }) => List&lt;int&gt;[1,2,4,5]
        /// </summary>
        public static List<T> Flatten<T>(params object[] items)
        {
            var list = new List<T>();
            foreach (var item in items)
            {
                if (item is T t)
                    list.Add(t);
                else if (item is IEnumerable<T> ie)
                    list.AddRange(ie);
            }

            return list;
        }

        /// <summary>
        ///     Used in a LINQ 'where' predicate to invert the result,
        ///     e.g. Animes.Where(Not{Anime}(Anime.HasId)) for all anime that dont have an id
        /// </summary>
        public static Func<T, bool> Not<T>(Func<T, bool> method) => args => !method(args);

        /// <summary>
        ///     Used in [Option].Map chaining to call void function {f} with arguments {T1} and {T2} and
        ///     then return the first argument,
        ///     e.g. Option.map(T1 => Tee(f, T1, T2)).MatchSome(Console.WriteLine) => writes `T1`
        /// </summary>
        public static T1 Tee<T1, T2>(Action<T1, T2> f, T1 arg, T2 arg2)
        {
            f(arg, arg2);
            return arg;
        }

        /// <summary>
        ///     Applies {func&lt;T,T&gt;} on {item&lt;T&gt;} for {amount} of items, returning the final result.
        ///     e.g. Apply(i => i + 1, 0, 5) => 5
        /// </summary>
        public static T Apply<T>(Func<T, T> func, T item, int amount = 1)
        {
            var that = item;
            for (var i = 0; i < Math.Max(amount, 0); i++)
                that = func(that);
            return that;
        }

        public static class List
        {
            public static List<T> Of<T>(params T[] items)
            {
                var list = new List<T>();
                list.AddRange(items);
                return list;
            }
        }

        public static class Enumerable
        {
            public static IEnumerable<T> Of<T>(params T[] items) => items;
        }

        public static List<T> GetValues<T>() where T : struct => Enum.GetValues(typeof(T)).Cast<T>().ToList();

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
            if (filename is null)
                return null;

            // Remove brackets and parenthesis
            filename = Regex.Replace(filename, @"\s?\[(.*?)\]|\((.*?)\)\s*", "");

            // Remove extensions
            filename = Regex.Replace(filename, @"\.mkv|\.mp4|\.avi", "");

            // Replace any "Episode 08" with just "08"
            var episode = Regex.Match(filename, @"[\-\s[\s_.]?(episode[\s_.]?(\d{1,3}))", RegexOptions.IgnoreCase);
            if (episode.Success)
                filename = filename.Replace(episode.Groups[1].Value, episode.Groups[2].Value);

            // Remove Season 1 / S1 stuff
            filename = Regex.Replace(filename, @"\b(s[1-9]\d?|season [1-9]\d?)\b", "", RegexOptions.IgnoreCase);

            // Remove entirely anything saying the language sub
            filename = Regex.Replace(filename, @"-?[\s_.]?eng(?:lish|s)?[\s_.]?sub(?:bed|s)?", "", RegexOptions.IgnoreCase);

            // _ and . can be used for spaces for some subgroups, replace with spaces instead
            filename = new[] { "_", "." }.Aggregate(filename, (current, s) => current.Replace(s, " "));

            // Remove annoying fps counters
            filename = Regex.Replace(filename, @"\d{2,}\s?fps", "", RegexOptions.IgnoreCase);

            // Remove episode names / phrases after the episode counter
            var name = Regex.Match(filename, @"\s\d{1,}(\s-\s[\w'\s]+)$", RegexOptions.IgnoreCase);
            if (name.Success)
                filename = filename.Replace(name.Groups[1].Value, "");

            if (removeEpisode)
            {
                var regularEpisodePattern = Regex.Matches(filename, @"\-\s[0-9]{1,}"); // Name {- #}
                var namedEpisodePattern = Regex.Matches(filename, @"episode\s[0-9]{1,}", RegexOptions.IgnoreCase); // Name {Episode #}

                if (regularEpisodePattern.Count > 0)
                {
                    var split = filename.Split('-');
                    filename = Join("-", split.Where(c => !Regex.IsMatch(c, @"^\s[0-9]{1,}")));
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
            await DialogHost.Show(new MessageViewModel { Text = msg });
        }

        public static async Task<bool> QuestionYesNo(string question)
        {
            var result = true;
            await DialogHost.Show(new QuestionViewModel { Message = question }, (sender, args) =>
              {
                  result = (bool)args.Parameter;
              });
            return result;
        }

        public static void MoveFile(AnimeFile file, string startPath, string movePath)
        {
            if (!File.Exists(file.Path))
                return;

            var relative = Join(Path.DirectorySeparatorChar.ToString(),
                file.Path.Split(Path.DirectorySeparatorChar)
                    .Skip(startPath.Split(Path.DirectorySeparatorChar).Length));
            var newPath = Path.Combine(movePath, relative);
            var fileDepth = relative.Split(Path.DirectorySeparatorChar);
            if (fileDepth.Length > 1)
            {
                var added = Join(Path.DirectorySeparatorChar.ToString(),
                    fileDepth.Take(fileDepth.Length - 1));
                Directory.CreateDirectory(Path.Combine(movePath, added));
            }

            try
            {
                Directory.Move(file.Path, newPath);
            }

            catch
            {
                // ignored
            }
        }

        private static bool ValidPossiblePath(string path)
        {
            if (IsNullOrWhiteSpace(path) || path.Length <= 2)
                return false;

            FileInfo fi = null;

            try
            {
                fi = new FileInfo(path);
            }

            catch (ArgumentException) { }
            catch (PathTooLongException) { }
            catch (NotSupportedException) { }

            return !(fi is null);
        }

        public static async Task<bool> CheckDirectory(string path, string title)
        {
            if (Directory.Exists(path))
                return true;

            // Ask to create path
            if (ValidPossiblePath(path))
            {
                if (await QuestionYesNo($"Your '{title}' folder doesn't seem to exist.\n" +
                                        $"Would you like to create it at the given path:\n\n{path}"))
                    Directory.CreateDirectory(path);
            }

            // Illegal path
            else
            {
                Alert($"Your path for the {title} folder is invalid, try and enter it again.");
                return false;
            }

            return false;
        }

        public static bool CheckExe(string path, string title)
        {
            if (File.Exists(path) && path.ToLower().EndsWith(".exe"))
                return true;

            Alert($"Your path for the {title} is invalid, try and enter it again.");
            return false;
        }

        public static bool NotNullDifferent(string original, string comparison) => (original?.ToLower() ?? "") != (comparison?.ToLower() ?? "");

        public static Action None = () => { };
    }
}