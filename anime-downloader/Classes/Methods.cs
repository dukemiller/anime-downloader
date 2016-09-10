using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

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
            { }
            for (var j = 0; j <= m; d[0, j] = j++)
            { }
            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = t[j - 1] == s[i - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        /// <summary>
        ///     Strip the entire path of extraneous information (subgroups, resolution, etc).
        /// </summary>
        /// <param name="filename">
        /// A file name, not a filepath.
        /// </param>
        /// <param name="removeEpisode">
        ///  A flag for also removing the episode number
        /// </param>
        public static string Strip(string filename, bool removeEpisode = false)
        {
            var text = filename;

            var phrases = (from Match match in Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*")
                select match.Groups[0].Value).ToList();

            phrases.ForEach(p => text = text.Replace(p, ""));

            text = text.Replace("_", " ");
            text = new[] { ".mkv", ".mp4", ".avi" }.Aggregate(text, (current, s) => current.Replace(s, ""));

            if (removeEpisode)
                text = String.Join("-", text.Split('-').Take(text.Split('-').Length - 1).ToArray());
            
            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        ///     Completely clear focus from an element.
        /// </summary>
        public static void ClearFocusFrom(FrameworkElement element)
        {
            var parent = (FrameworkElement) element.Parent;
            while (parent != null && !((IInputElement) parent).Focusable)
                parent = (FrameworkElement) parent.Parent;
            var scope = FocusManager.GetFocusScope(element);
            FocusManager.SetFocusedElement(scope, parent);
        }

        /// <summary>
        ///     Display an alert message (currently a messagebox).
        /// </summary>
        public static void Alert(string msg = "") => MessageBox.Show(msg);
    }
}