using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;
using anime_downloader.Classes.Web.MyAnimeList;
using anime_downloader.Enums;

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

        private async Task SetToLastAsync(IEnumerable<Anime> animes, EpisodeStatus episodeStatus)
        {
            foreach (var anime in animes)
            {
                var episodes = await _animeFileCollection.GetEpisodesFromAsync(anime, episodeStatus);
                var lastEpisode = AnimeFileCollection.LastEpisodeOf(episodes);
                if (lastEpisode != null)
                    anime.Episode = lastEpisode.Episode;
            }
        }

        private async Task SetToFirstAsync(IEnumerable<Anime> animes, EpisodeStatus episodeStatus)
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

        public async Task HandleCommand(string command)
        {
            var airingAnime = MainWindow.Window.AllAnime.AiringAndWatching();

            switch (command)
            {

                case "Duplicates":
                {
                    Methods.Alert($"Moved {await MoveDuplicatesAsync()} files to duplicate folder.");
                    break;
                }

                case "LastWatched":
                {
                    await SetToLastAsync(airingAnime, EpisodeStatus.Watched);
                    Methods.Alert("Reset episode order to last known in watched folder.");
                    break;
                }

                case "LastUnwatched":
                {
                    await SetToLastAsync(airingAnime, EpisodeStatus.Unwatched);
                    Methods.Alert("Reset episode order to last known in episode folder.");
                    break;
                }

                case "LastAny":
                {
                    await SetToLastAsync(airingAnime, EpisodeStatus.All);
                    Methods.Alert("Reset episode order to last known in any folder.");
                    break;
                }

                case "FirstWatched":
                {
                    await SetToFirstAsync(airingAnime, EpisodeStatus.All);
                    Methods.Alert("Reset episode count to first known episode.");
                    break;
                }

                case "Zero": 
                {
                    foreach (var anime in airingAnime)
                        anime.Episode = "00";
                    Methods.Alert("Reset episode count to zero.");
                    break;
                }

                case "MarkComplete":
                {
                    var names = new List<string>();
                    foreach (var anime in airingAnime
                        .Where(a => a.MyAnimeList.HasId && (
                                        (a.MyAnimeList.IntOverallTotal() > 0 &&
                                         a.IntEpisode() == a.MyAnimeList.IntOverallTotal()) ||
                                        (a.MyAnimeList.IntTotalEpisodes() > 0 &&
                                         a.IntEpisode() == a.MyAnimeList.IntTotalEpisodes())
                                    )))
                    {
                        anime.Status = "Finished";
                        anime.Airing = false;
                        names.Add(anime.Title);
                    }

                    var result = names.Count > 0 ? string.Join(", ", names) : "no shows";
                    Methods.Alert($"Marked {result} as finished. ");
                    break;
                }

                case "SearchMore":
                {
                    var credentials = Api.GetCredentials(MainWindow.Window.Settings);
                    var updated = new List<string>();
                    var animesMissingTotal = airingAnime
                        .Where(a => a.MyAnimeList.HasId &&
                                    (a.MyAnimeList.IntTotalEpisodes() == 0))
                        .ToList();

                    foreach (var anime in animesMissingTotal)
                    {
                        var animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(anime.Title));
                        var result = animeResults.FirstOrDefault(r => r.Id.Equals(anime.MyAnimeList.Id));

                        if (result != null && !anime.MyAnimeList.TotalEpisodes.Equals(result.TotalEpisodes))
                        {
                            updated.Add(anime.Title);
                            anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;
                        }
                    }

                    if (updated.Count > 0)
                    {
                        var updateResult = string.Join(", and ", updated);
                        Methods.Alert($"Updated total episodes for {updateResult}.");
                    }

                    else
                    {
                        Methods.Alert($"No shows were updated for an attempted {animesMissingTotal.Count} shows.");
                    }

                    break;
                }

                default:
                    break;

            }
        }
    }
}