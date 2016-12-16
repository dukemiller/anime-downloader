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

            EpisodeRange = Enumerable
                .Range(start.IntEpisode, end.IntEpisode - start.IntEpisode + 1)
                .Select(n => $"{n:00}");

            Name = start.Name;
        }

        public IEnumerable<string> EpisodeRange { get; }

        public string Name { get; }
    }
}