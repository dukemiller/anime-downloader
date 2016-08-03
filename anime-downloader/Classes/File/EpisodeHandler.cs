using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace anime_downloader.Classes.File
{

    /// <summary>
    ///     A class meant to handle any potential WRITE operations on file paths
    /// </summary>
    public class EpisodeHandler
    {

        private readonly AnimeFileCollection _animeFileCollection;

        public EpisodeHandler(Settings settings)
        {
            _animeFileCollection = new AnimeFileCollection(settings);
        }

        public async Task SetToLastAsync(IEnumerable<Anime> animes, EpisodeStatus episodeStatus)
        {
            foreach (var anime in animes)
            {
                var episodes = await _animeFileCollection.GetEpisodesFromAsync(anime, episodeStatus);
                var lastEpisode = AnimeFileCollection.LastEpisodeOf(episodes);
                if (lastEpisode != null)
                    anime.Episode = lastEpisode.Episode;
            }
        }

        public async Task SetToFirstAsync(IEnumerable<Anime> animes, EpisodeStatus episodeStatus)
        {
            foreach (var anime in animes)
            {
                var episodes = await _animeFileCollection.GetEpisodesFromAsync(anime, episodeStatus);
                var firstEpisode = AnimeFileCollection.FirstEpisodeOf(episodes);
                if (firstEpisode != null)
                    anime.Episode = firstEpisode.Episode;
            }
        }

        /// <summary>
        ///     Moves all selected items in listbox from old to new directory returning a list containing tuples of (old, new).
        /// </summary>
        public static IEnumerable<Tuple<AnimeFile, AnimeFile>> MoveEpisodesToDestination(ListBox list, string oldDirectory, string newDirectory)
        {

            var episodes = list.SelectedItems.Cast<AnimeFile>();
            var newEpisodes = new List<Tuple<AnimeFile, AnimeFile>>();

            foreach (var episode in episodes)
            {
                try
                {
                    var fullPath = episode.Path.Replace(oldDirectory, "").Substring(1);
                    var fragments = fullPath.Split(Path.DirectorySeparatorChar);

                    var oldPath = oldDirectory;
                    var newPath = newDirectory;

                    // If file is a directory, create in new folder, else move file
                    foreach (var fragment in fragments)
                    {
                        oldPath = Path.Combine(oldPath, fragment);
                        newPath = Path.Combine(newPath, fragment);

                        if (Directory.Exists(oldPath))
                            Directory.CreateDirectory(newPath);
                        else if (System.IO.File.Exists(oldPath))
                            System.IO.File.Move(oldPath, newPath);
                    }

                    // Delete all old folders (this would only work on the latest one because it's the only
                    // path that would be empty, TODO: fix that
                    oldPath = oldDirectory;
                    foreach (var fragment in fragments)
                    {
                        oldPath = Path.Combine(oldPath, fragment);
                        if (Directory.Exists(oldPath))
                            if (Directory.GetFiles(oldPath).Length == 0)
                            {
                                Directory.Delete(oldPath);
                                break;
                            }
                    }

                    newEpisodes.Add(new Tuple<AnimeFile, AnimeFile>(episode, new AnimeFile(newPath)));
                }

                catch (Exception)
                {
                    //
                }
            }

            return newEpisodes;
        }

        public async Task<int> MoveDuplicatesAsync()
        {
            var animeEpisodes = (await _animeFileCollection.GetEpisodesAsync(EpisodeStatus.Unwatched)).ToList();

            // if there's another anime with the same name and episode count,
            // and it's not in the duplicate list already
            var duplicates = animeEpisodes.Where(episode =>
                animeEpisodes.Any(otherEpisode => episode != otherEpisode &&
                                                  episode.Name.Equals(otherEpisode.Name) &&
                                                  episode.IntEpisode == otherEpisode.IntEpisode
                    )).ToList();

            if (duplicates.Any())
            {
                foreach (var duplicate in duplicates)
                    System.IO.File.Move(duplicate.Path,
                        Path.Combine(Settings.DuplicatesDirectory, duplicate.FileName));
            }

            return duplicates.Count;
        }
        
    }
}