using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace anime_downloader.Classes.File
{
    public class Playlist
    {
        private readonly Handler _handler;
        private IEnumerable<IndividualEpisode> _episodes;

        public Playlist(Handler handler)
        {
            _handler = handler;
        }

        public string PlaylistFile => Settings.PlaylistFile;

        public int Length => _episodes.Count();

        /// <summary>
        ///     Re-initialize the collection of episodes from the folders.
        /// </summary>
        public void Refresh()
        {
            _episodes = _handler.Episodes(EpisodeType.Unwatched);
        }

        public void Refresh(IEnumerable<IndividualEpisode> episodes)
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
            var sortedEpisodes = new List<IndividualEpisode>();
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
        public async Task Save()
        {
            using (var writer = new StreamWriter(Settings.PlaylistFile, false))
            {
                foreach (var episode in _episodes)
                    await writer.WriteLineAsync(episode.FilePath);
            }
        }
    }
}