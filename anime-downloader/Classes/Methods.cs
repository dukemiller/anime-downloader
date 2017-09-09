using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
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
            var text = filename;

            // Remove brackets and parenthesis
            var phrases = Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*")
                .Cast<Match>()
                .Select(match => match.Groups[0].Value)
                .ToList();
            text = new[] {".mkv", ".mp4", ".avi"}.Union(phrases).Aggregate(text, (current, s) => current.Replace(s, ""));

            // Replace any "Episode 08" with just "08"
            var episode = Regex.Match(text, @"-[\s_.]?(episode[\s_.]?(\d{1,3}))", RegexOptions.IgnoreCase);
            if (episode.Success)
                text = text.Replace(episode.Groups[1].Value, episode.Groups[2].Value);

            // Remove entirely anything saying the language sub
            text = Regex.Replace(text, @"-?[\s_.]?eng(?:lish|s)?[\s_.]?sub(?:bed|s)?", "", RegexOptions.IgnoreCase);

            // _ and . can be used for spaces for some subgroups, replace with spaces instead
            text = new[] {"_", "."}.Aggregate(text, (current, s) => current.Replace(s, " "));

            if (removeEpisode)
            {
                var regularEpisodePattern = Regex.Matches(text, @"\-\s[0-9]{1,}"); // Name {- #}
                var namedEpisodePattern = Regex.Matches(text, @"episode\s[0-9]{1,}", RegexOptions.IgnoreCase); // Name {Episode #}

                if (regularEpisodePattern.Count > 0)
                {
                    var split = text.Split('-');
                    text = string.Join("-", split.Take(split.Length - 1));
                }

                else if (namedEpisodePattern.Count > 0)
                {
                    var value =
                        namedEpisodePattern.Cast<Match>().Select(match => match.Groups[0].Value).ToList().First();
                    text = text.Replace(value, "");
                }
            }

            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        ///     Display an alert message (currently a messagebox).
        /// </summary>
        public static async void Alert(string msg = "")
        {
            await DialogHost.Show(new DialogViewModel {Message = msg});
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

        public static void AnimeRatingRules(TextBox textbox, TextCompositionEventArgs e)
        {
            if (textbox.Text.Any(c => !char.IsDigit(c)) || e.Text.Any(c => !char.IsDigit(c)) ||
                e.Text.Length == 0 || e.Text.Trim().Equals(" ") || string.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }

            if (textbox.Text.Length == 0)
                return;

            var current = int.Parse(textbox.Text);
            var adder = int.Parse(e.Text);

            if (current == 10)
            {
                if (textbox.SelectionStart == 2)
                {
                    e.Handled = true;
                    textbox.Text = $"{adder}";
                    textbox.SelectionStart = 1;
                }

                else if (textbox.SelectedText.Length != textbox.Text.Length)
                {
                    e.Handled = true;
                }
            }

            else
            {
                if (adder == 0)
                    if (current == 1)
                    {
                        textbox.Text = "10";
                        textbox.SelectionStart = 2;
                        e.Handled = true;
                        return;
                    }
                e.Handled = true;
                textbox.Text = $"{adder}";
                textbox.SelectionStart = 1;
            }
        }

        public static int Mod(int x, int m) => (x % m + m) % m;

        public static bool InRange(int number, int inclusiveBottom, int inclusiveTop) => number >= inclusiveBottom &&
                                                                                         number <= inclusiveTop;

    }
}