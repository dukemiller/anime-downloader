using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class PlaylistService : IPlaylistService
    {

        private readonly ISettingsService _settings;

        public PlaylistService(ISettingsService settings, IFileService file)
        {
            _settings = settings;
            File = file;
        }

        public IFileService File { get; set; }

        public IEnumerable<AnimeFile> Episodes { get; set; }

        public int Length => Episodes.Count();

        public string Path => _settings.PathConfig.Playlist;

        // 

        public void Refresh() => Episodes = File.GetEpisodes(EpisodeStatus.Unwatched);

        public void Set(IEnumerable<AnimeFile> files)
        {
            Episodes = files;
        }

        public void OrderByEpisodeNumber() => Episodes = Episodes.OrderBy(f => f.Episode);

        public void OrderByDate() => Episodes = Episodes.OrderBy(e => System.IO.File.GetCreationTime(e.Path));

        public void ReverseOrder() => Episodes = Episodes.Reverse();

        public void SeparateShowOrder()
        {
            var sortedEpisodes = new List<AnimeFile>();
            var currentEpisodes = Episodes.ToList();

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

            Episodes = sortedEpisodes;
        }

        public void AdditionalEpisodesFirst()
        {
            var episodes = Episodes.ToList();
            var names = episodes.GroupBy(e => e.Name)
                .Where(grp => grp.Count() > 1)
                .SelectMany(group =>
                {
                    var count = group.Count();
                    return group.Take(count - 1);
                }).ToList();
            var originals = episodes.Except(names);
            Episodes = names.Concat(originals);
        }

        public async Task<string> Create()
        {
            using (var writer = new StreamWriter(_settings.PathConfig.Playlist, false))
            {
                foreach (var episode in Episodes)
                    await writer.WriteLineAsync(episode.Path);
            }

            return _settings.PathConfig.Playlist;
        }
    }
}