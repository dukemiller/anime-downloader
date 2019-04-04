using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    /// <summary>
    ///     A playlist of AnimeFiles with modifiable sorting options and configurations.
    /// </summary>
    public class Playlist : ObservableObject
    {
        /// <summary>
        ///     The collection of anime files.
        /// </summary>
        public List<AnimeFile> Source { get; set; } = new List<AnimeFile>();

        /// <summary>
        ///     Specify if any of the sorting options should be applied.
        /// </summary>
        public bool Sort { get; set; } = true;

        /// <summary>
        ///     If we're creating the playlist from an episode selection vs a playlist.
        /// </summary>
        public bool IsEpisodeSelection { get; set; }

        /// <summary>
        ///     The order of how the files will be initially sorted from Source.
        /// </summary>
        public PlaylistOrder Order { private get; set; }

        /// <summary>
        ///     Any other alteration flags on the Source will be modified.
        /// </summary>
        public PlaylistOptions Options { protected internal get; set; }

        /// <summary>
        ///     Determined path to the playlist file.
        /// </summary>
        public string Path => IsEpisodeSelection
            ? App.Path.Episodes
            : App.Path.Playlist;

        /// <summary>
        ///     Distribute the show order so that no show will appear twice in a row
        ///     if it's possible.
        /// </summary>
        private static IEnumerable<AnimeFile> SeparateShowOrder(IEnumerable<AnimeFile> source)
        {
            var sorted = new List<AnimeFile>();
            var current = source.ToList();

            while (current.Count > 0)
            {
                // remove everything from sorted
                current.RemoveAll(file => sorted.Contains(file));
                var added = new List<string>();

                // go through the list sequentially, if the name hasn't 
                // already appeared then add it to the list once and 
                // add it and skip any future appearance for this loop
                foreach (var file in current)
                {
                    var show = file.Name;
                    if (added.Contains(show))
                        continue;
                    sorted.Add(file);
                    added.Add(show);
                }
            }

            return sorted;
        }

        /// <summary>
        ///     Distribute the show order so that any show that has more than one
        ///     episode will appear on the list first.
        /// </summary>
        private static IEnumerable<AnimeFile> AdditionalEpisodesFirst(IEnumerable<AnimeFile> source, bool separateOrder)
        {
            var files = source.ToList();

            // For every series that has more than one episode, get every episode
            // except the last episode
            var names = files.GroupBy(e => e.Name)
                .Where(group => group.Count() > 1)
                .SelectMany(group =>
                {
                    var count = group.Count();
                    return group.Take(count - 1);
                }).ToList();

            // Take the segmented files, and now put the last of every 
            // series at the end of the list
            var originals = files.Where(file1 => !names.Any(file2 => file2.FileName.Equals(file1.FileName)));

            return separateOrder
                ? SeparateShowOrder(names).Concat(SeparateShowOrder(originals))
                : names.Concat(originals);
        }

        /// <summary>
        ///     Apply the changes in Order and Options to the Source.
        /// </summary>
        public void ApplyConfiguration()
        {
            IEnumerable<AnimeFile> stream;

            switch (Order)
            {
                case PlaylistOrder.Date:
                    stream = Source.OrderBy(file => File.GetCreationTime(file.Path));
                    break;
                case PlaylistOrder.NameThenEpisode:
                    stream = Source.OrderBy(file => file.Name).ThenBy(file => file.Episode);
                    break;
                case PlaylistOrder.EpisodeThenName:
                    stream = Source.OrderBy(file => file.Episode).ThenBy(file => file.Name);
                    break;
                case PlaylistOrder.Default:
                    stream = Source.OrderBy(file => file.FileName, new WindowsSortComparer());
                    break;
                case PlaylistOrder.RandomNameThenEpisode:
                    stream = Source.OrderBy(file => file.Name).ThenBy(file => file.Episode).GroupBy(e => e.Name).Shuffle().SelectMany(e => e);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (Options.HasFlag(PlaylistOptions.AdditionalEpisodesFirst))
                stream = AdditionalEpisodesFirst(stream, Options.HasFlag(PlaylistOptions.SeparateShowOrder));

            else if (Options.HasFlag(PlaylistOptions.SeparateShowOrder))
                stream = SeparateShowOrder(stream);

            if (Options.HasFlag(PlaylistOptions.Reverse))
                stream = stream.Reverse();

            Source = stream.ToList();
        }

        /// <summary>
        ///     Take the paths from Source and create the playlist.
        /// </summary>
        public async Task<bool> Create()
        {
            var successful = false;

            try
            {
                if (Sort)
                    ApplyConfiguration();
                
                using (var writer = new StreamWriter(Path, false))
                    foreach (var file in Source)
                        await writer.WriteLineAsync(file.Path);

                successful = true;
            }

            catch
            {
                // ignored
            }

            return successful;
        }
    }
}