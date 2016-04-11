using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace anime_downloader.Classes.File
{
    /// <summary>
    ///     All the details of an anime file as it's interpreted on the filesystem.
    /// </summary>
    public class AnimeEpisode
    {
        public AnimeEpisode(string filePath)
        {
            FilePath = filePath;
        }

        /// <summary>
        ///     The anime name gathered from the filename.
        /// </summary>
        public string Name
        {
            get
            {
                return string.Join("-",
                    StrippedFilename.Split('-')
                        .Take(StrippedFilename.Count(x => x == '-')))
                    .Trim();
            }
        }

        // TODO: man wow
        /// <summary>
        ///     The episode gathered from the filename.
        /// </summary>
        public string Episode
        {
            get
            {
                var number =
                    int.Parse(string.Join("",
                        StrippedFilename.Split(new[] {" - "}, StringSplitOptions.RemoveEmptyEntries)
                            .Last().TakeWhile(char.IsNumber)));
                return $"{number:00}";
            }
        }

        /// <summary>
        ///     Integer parsed episode.
        /// </summary>
        public int IntEpisode => int.Parse(Episode);

        /// <summary>
        ///     The meta information stripped filename (no seeders, subgroups, etc)
        /// </summary>
        private string StrippedFilename => Strip(FileName);

        /// <summary>
        ///     The path's filename, e.g. "C:/.../.../{anime.mp4}".
        /// </summary>
        public string FileName => Path.GetFileName(FilePath);

        /// <summary>
        ///     The full path filename.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        ///     Strip the entire path of extraneous information (subgroups, resolution, etc).
        /// </summary>
        /// <param name="fileName">A file name, not a filepath.</param>
        /// <returns></returns>
        private static string Strip(string fileName)
        {
            var text = fileName;

            var phrases = (from Match match in Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*")
                select match.Groups[0].Value).ToList();

            new[] {"_", ".mp4", ".mkv", ".avi"}.ToList().ForEach(p => phrases.Add(p));

            phrases.ForEach(p => text = text.Replace(p, ""));

            // text = string.Join("-", text.Split('-').Take(text.Split('-').Length - 1).ToArray());

            return Regex.Replace(text.Trim(), @"\s+", " ");
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
}