using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace anime_downloader.Classes.File
{
    public class Playlist
    {
        private readonly FileHandler _fileHandler;
        private readonly Settings _settings;
        private IEnumerable<AnimeEpisode> _episodes;

        public Playlist(Settings settings, FileHandler fileHandler)
        {
            _settings = settings;
            _fileHandler = fileHandler;
        }

        public string PlaylistFile => _settings.PlaylistFile;

        public int Length => _episodes.Count();

        /// <summary>
        ///     Re-initialize the collection of episodes from the folders.
        /// </summary>
        public void Refresh()
        {
            _episodes = _fileHandler.UnwatchedAnimeEpisodes();
        }

        public void Refresh(IEnumerable<AnimeEpisode> episodes)
        {
            _episodes = episodes;
        }

        /// <summary>
        ///     Do a more rigid sort by episode number of the show.
        /// </summary>
        public void ByEpisodeNumber()
        {
            _episodes = _episodes.OrderBy(f => f.IntEpisode);
        }

        /// <summary>
        ///     Sort simply by the time the file was created.
        /// </summary>
        public void ByDate()
        {
            _episodes = _episodes.OrderBy(e => System.IO.File.GetCreationTime(e.FilePath));
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
            var sortedEpisodes = new List<AnimeEpisode>();
            var currentEpisodes = _episodes.ToList();

            while (currentEpisodes.Count > 0)
            {
                currentEpisodes.RemoveAll(e => sortedEpisodes.Contains(e));
                var addedShows = new List<string>();
                foreach (var episode in currentEpisodes)
                {
                    var show = episode.Name;
                    if (addedShows.Contains(show))
                        continue;
                    sortedEpisodes.Add(episode);
                    addedShows.Add(show);
                }
            }

            _episodes = sortedEpisodes;
        }

        /// <summary>
        ///     Save and create the playlist.
        /// </summary>
        public async void Save()
        {
            using (var writer = new StreamWriter(_settings.PlaylistFile, false))
            {
                foreach (var episode in _episodes)
                    await writer.WriteLineAsync(episode.FilePath);
            }
        }
    }
}