using System;
using System.Linq;

namespace anime_downloader.Classes.File
{
    /// <summary>
    ///     All the details of an anime file as it's interpreted on the filesystem.
    /// </summary>
    public class AnimeFile : IComparable<AnimeFile>
    {
        public AnimeFile(string path)
        {
            Path = path;
        }

        /// <summary>
        ///     The anime name gathered from the filename, e.g. "{Show} - 01.mp4"
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

        /// <summary>
        ///     The episode gathered from the filename, e.g. "Show - {01}.mp4".
        /// </summary>
        public string Episode
        {
            get
            {
                var _ = string.Join("", StrippedFilename.Replace(" ", "")
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
        public int CompareTo(AnimeFile other)
        {
            return string.Compare(StrippedFilename, other.StrippedFilename, StringComparison.Ordinal);
        }
    }
}