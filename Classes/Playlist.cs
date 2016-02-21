using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace anime_downloader.Classes {
    public class Playlist {
        public IEnumerable<string> episodes;
        private readonly Settings settings;

        public Playlist(Settings settings) {
            this.settings = settings;
            refresh();
        }

        /// <summary>
        ///     A path to where the playlist will be created.
        /// </summary>
        private string path {
            get { return Path.Combine(settings.baseFolderPath, "playlist.m3u"); }
        }

        /// <summary>
        ///     Re-initialize the collection of episodes from the folders.
        /// </summary>
        public void refresh() {
            episodes = Directory.GetDirectories(settings.baseFolderPath)
                .Where(s => !s.EndsWith("torrents") && !s.EndsWith("Grace") && !s.EndsWith("Watched"))
                .SelectMany(f => Directory.GetFiles(f))
                .Where(f => !isFragmentedVideo(f));
        }

        /// <summary>
        ///     Do a more rigid sort by episode number of the show.
        /// </summary>
        public void byEpisodeNumber() {
            episodes = episodes.OrderBy(f => strip(Path.GetFileName(f)));
        }

        /// <summary>
        ///     Sort simply by the time the file was created.
        /// </summary>
        public void byDate() {
            episodes = episodes.OrderBy(f => File.GetCreationTime(f));
        }

        /// <summary>
        ///     Distribute the show order so that you don't watch the same show twice in a row.
        /// </summary>
        public void separateShowOrder() {
            var sortedEpisodes = new List<string>();
            var currentEpisodes = episodes.ToList();
            List<string> addedShows;

            while (currentEpisodes.Count > 0) {
                currentEpisodes.RemoveAll(e => sortedEpisodes.Contains(e));
                addedShows = new List<string>();
                foreach (var episode in currentEpisodes) {
                    var show = strip(Path.GetFileName(episode), true);
                    if (addedShows.Contains(show))
                        continue;
                    sortedEpisodes.Add(episode);
                    addedShows.Add(show);
                }
            }

            episodes = sortedEpisodes;
        }

        /// <summary>
        ///     Check if the file is fragmented by some byte guesswork.
        /// </summary>
        /// <param name="filepath">Full path to the file.</param>
        /// <returns></returns>
        private bool isFragmentedVideo(string fullFilepath) {
            byte currentByte;
            var counter = 0;

            try {
                using (var reader = new BinaryReader(File.Open(fullFilepath, FileMode.Open))) {
                    currentByte = reader.ReadByte();
                    while (currentByte == 0) {
                        if (++counter > 10)
                            break;
                        currentByte = reader.ReadByte();
                    }
                }
            }

            catch (IOException e) {
                return true;
            }

            return !(currentByte > 10);
        }

        /// <summary>
        ///     Strip the entire path of extraneous information (subgroups, resolution, etc).
        /// </summary>
        /// <param name="fileName">A file name, not a filepath.</param>
        /// <param name="removeEpisode">A flag to also optionally remove the episode number.</param>
        /// <returns></returns>
        private string strip(string fileName, bool removeEpisode = false) {
            var phrases = new List<string>();
            var text = fileName;

            foreach (Match match in Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*"))
                phrases.Add(match.Groups[0].Value);

            foreach (var phrase in phrases)
                text = text.Replace(phrase, "");

            if (removeEpisode)
                text = string.Join("-", text.Split('-').Take(text.Split('-').Length - 1).ToArray());

            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        ///     Save and create the playlist.
        /// </summary>
        public void save() {
            using (var writer = new StreamWriter(path, false)) {
                foreach (var episode in episodes)
                    writer.WriteLine(episode);
            }
        }
    }
}