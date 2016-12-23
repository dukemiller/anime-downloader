using System;
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
        public PlaylistService(ISettingsService settings, IAnimeFileService animeFile)
        {
            Settings = settings;
            AnimeFile = animeFile;
        }

        private ISettingsService Settings { get; set; }

        public IAnimeFileService AnimeFile { get; set; }

        public IEnumerable<AnimeFile> Episodes { get; set; }

        public int Length => Episodes.Count();

        public string Path => Settings.PathConfig.Playlist;

        // 

        public void Refresh() => Episodes = AnimeFile.GetEpisodes(EpisodeStatus.Unwatched);

        public void Set(IEnumerable<AnimeFile> files)
        {
            Episodes = files;
        }

        public void OrderByEpisodeNumber() => Episodes = Episodes.OrderBy(f => f.Episode);

        public void OrderByDate() => Episodes = Episodes.OrderBy(e => File.GetCreationTime(e.Path));

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

        public async Task<string> Create()
        {
            using (var writer = new StreamWriter(Settings.PathConfig.Playlist, false))
            {
                foreach (var episode in Episodes)
                    await writer.WriteLineAsync(episode.Path);
            }

            return Settings.PathConfig.Playlist;
        }
    }
}