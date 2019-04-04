using System;
using System.Linq;
using anime_downloader.Classes;

namespace anime_downloader.Models
{
    /// <summary>
    ///     All the details of an anime file as it's interpreted on the filesystem.
    /// </summary>
    public class AnimeFile : IComparable<AnimeFile>
    {
        /// <param name="path">The full path filename, e.g. "{C:/.../.../anime.mp4}".</param>
        public AnimeFile(string path) => Path = path;

        /// <summary>
        ///     The anime name gathered from the filename, e.g. "{Show} - 01.mp4"
        /// </summary>
        public string Name
        {
            get
            {
                var delimiter = StrippedFilename.Count(c => c == '-') > 0 ? '-' : ' ';
                var count = StrippedFilename.Count(c => c == delimiter);
                return string.Join(delimiter.ToString(), StrippedFilename.Split(delimiter).Take(count)).Trim();
            }
        }

        /// <summary>
        ///     The episode gathered from the filename, e.g. "Show - {01}.mp4".
        /// </summary>
        public int Episode
        {
            get
            {
                var episode = 0;

                if (StrippedFilename.Any(char.IsDigit))
                {
                    if (StrippedFilename.Contains("-"))
                    {
                        var _ = string.Join("",
                            StrippedFilename.Replace(" ", "")
                                .Split('-')
                                .Last(stripped => stripped.Any(char.IsNumber))
                                .TakeWhile(char.IsNumber)
                        );

                        var result = int.TryParse(_, out var number);
                        episode = result ? number : 0;
                    }

                    else
                    {
                        // Work backwords from the last phrase, taking any token that is only numbers
                        var _ = StrippedFilename.Split(' ')
                            .Reverse()
                            .SkipWhile(chunk => !chunk.All(char.IsDigit))
                            .FirstOrDefault();
                        var result = int.TryParse(_, out var number);
                        episode = result ? number : 0;
                    }
                }

                return episode;
            }
        }

        /// <summary>
        ///     The meta information stripped filename (no seeders, subgroups, etc)
        /// </summary>
        public string StrippedFilename => Methods.Strip(FileName);

        /// <summary>
        ///     The path's filename, e.g. "C:/.../.../{anime.mp4}".
        /// </summary>
        public string FileName => System.IO.Path.GetFileName(Path);

        /// <summary>
        ///     The full path filename, e.g. "{C:/.../.../anime.mp4}".
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     Comparator used for the AddSorted extension method
        /// </summary>
        public int CompareTo(AnimeFile other) =>
            string.Compare(StrippedFilename, other.StrippedFilename, StringComparison.Ordinal);
    }
}