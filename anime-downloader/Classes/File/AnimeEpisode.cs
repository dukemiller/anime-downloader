using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static System.String;

namespace anime_downloader.Classes.File
{
    /// <summary>
    ///     All the details of an anime file as it's interpreted on the filesystem.
    /// </summary>
    public class AnimeEpisode : IComparable<AnimeEpisode>
    {
        public AnimeEpisode(string filePath)
        {
            FilePath = filePath;
        }

        /// <summary>
        ///     The anime name gathered from the filename, e.g. "{Show} - 01.mp4"
        /// </summary>
        public string Name
        {
            get
            {
                return Join("-",
                    StrippedFilename.Split('-')
                        .Take(StrippedFilename.Count(x => x == '-')))
                    .Trim();
            }
        }

        // TODO: man wow
        /// <summary>
        ///     The episode gathered from the filename, e.g. "Show - {01}.mp4".
        /// </summary>
        public string Episode
        {
            get
            {
                var _ = Join("", StrippedFilename.Replace(" ", "")
                    .Split('-')
                    .Last(stripped => stripped.Any(char.IsNumber))
                    .TakeWhile(char.IsNumber));
                int number;
                var result = int.TryParse(_, out number);
                var value = result ? number : 0;
                return $"{value:D2}";
            }
        }

        /// <summary>
        ///     Integer parsed episode.
        /// </summary>
        public int IntEpisode => int.Parse(Episode);

        /// <summary>
        ///     The meta information stripped filename (no seeders, subgroups, etc)
        /// </summary>
        public string StrippedFilename => Strip(FileName);

        /// <summary>
        ///     The path's filename, e.g. "C:/.../.../{anime.mp4}".
        /// </summary>
        public string FileName => Path.GetFileName(FilePath);

        /// <summary>
        ///     The full path filename, e.g. "{C:/.../.../anime.mp4}".
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        ///     Strip the entire path of extraneous information (subgroups, resolution, etc).
        /// </summary>
        /// <param name="fileName">A file name, not a filepath.</param>
        private static string Strip(string fileName)
        {
            var text = fileName;

            var phrases = (from Match match in Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*")
                           select match.Groups[0].Value).ToList();

            new[] { "_", ".mp4", ".mkv", ".avi" }.ToList().ForEach(p => phrases.Add(p));

            phrases.ForEach(p => text = text.Replace(p, ""));

            // text = string.Join("-", text.Split('-').Take(text.Split('-').Length - 1).ToArray());

            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        ///     Comparator used for the AddSorted extension method
        /// </summary>
        public int CompareTo(AnimeEpisode other)
        {
            return Compare(StrippedFilename, other.StrippedFilename, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     The difference between an anime's first and last episode with identifying information
    /// </summary>
    public class AnimeEpisodeDelta
    {
        public AnimeEpisodeDelta(AnimeEpisode a, AnimeEpisode b)
        {
            EpisodeRange = Enumerable.Range(a.IntEpisode, b.IntEpisode - a.IntEpisode + 1).Select(n => $"{n:00}");
            Name = a.Name;
        }

        public IEnumerable<string> EpisodeRange { get; }

        public string Name { get; }
    }

    public class AnimeWithEpisodes
    {
        public Anime Anime { get; set; }
        public IEnumerable<AnimeEpisode> Episodes { get; set; }
    }
}