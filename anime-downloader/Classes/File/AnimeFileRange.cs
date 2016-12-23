using System.Collections.Generic;
using System.Linq;
using anime_downloader.Models;

namespace anime_downloader.Classes.File
{
    /// <summary>
    ///     The difference between an anime's first and last episode with identifying information
    /// </summary>
    public class AnimeFileRange
    {
        public AnimeFileRange(AnimeFile start, AnimeFile end)
        {
            EpisodeRange = Enumerable.Range(start.Episode, end.Episode - start.Episode + 1);
            Name = start.Name;
        }

        public IEnumerable<int> EpisodeRange { get; }

        public string Name { get; }
    }
}