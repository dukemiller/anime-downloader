using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace anime_downloader.Classes.File
{
    public class Playlist
    {
        private readonly Settings _settings;
        private IEnumerable<string> _episodes;

        public Playlist(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        ///     Re-initialize the collection of episodes from the folders.
        /// </summary>
        public void Refresh()
        {
            _episodes = _settings.EpisodeDirectories()
                .SelectMany(Directory.GetFiles);
        }

        /// <summary>
        ///     Do a more rigid sort by episode number of the show.
        /// </summary>
        public void ByEpisodeNumber()
        {
            _episodes = _episodes.OrderBy(f => Strip(Path.GetFileName(f)));
        }

        /// <summary>
        ///     Sort simply by the time the file was created.
        /// </summary>
        public void ByDate()
        {
            _episodes = _episodes.OrderBy(System.IO.File.GetCreationTime);
        }

        public void Reverse()
        {
            _episodes = _episodes.Reverse();
        }

        /// <summary>
        ///     Distribute the show order so that you don't watch the same show twice in a row.
        /// </summary>
        public void SeparateShowOrder()
        {
            var sortedEpisodes = new List<string>();
            var currentEpisodes = _episodes.ToList();

            while (currentEpisodes.Count > 0)
            {
                currentEpisodes.RemoveAll(e => sortedEpisodes.Contains(e));
                var addedShows = new List<string>();
                foreach (var episode in currentEpisodes)
                {
                    var show = Strip(Path.GetFileName(episode), true);
                    if (addedShows.Contains(show))
                        continue;
                    sortedEpisodes.Add(episode);
                    addedShows.Add(show);
                }
            }

            _episodes = sortedEpisodes;
        }

        /// <summary>
        ///     Strip the entire path of extraneous information (subgroups, resolution, etc).
        /// </summary>
        /// <param name="fileName">A file name, not a filepath.</param>
        /// <param name="removeEpisode">A flag to also optionally remove the episode number.</param>
        /// <returns></returns>
        private static string Strip(string fileName, bool removeEpisode = false)
        {
            var text = fileName;

            var phrases = (from Match match in Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*")
                select match.Groups[0].Value).ToList();

            new[] {"_", ".mp4", ".mkv", ".avi"}.ToList().ForEach(p => phrases.Add(p));

            phrases.ForEach(p => text = text.Replace(p, ""));

            if (removeEpisode)
                text = string.Join("-", text.Split('-').Take(text.Split('-').Length - 1).ToArray());

            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        ///     Save and create the playlist.
        /// </summary>
        public async void Save()
        {
            using (var writer = new StreamWriter(_settings.PlaylistFile, false))
            {
                foreach (var episode in _episodes)
                    await writer.WriteLineAsync(episode);
            }
        }
    }
}