using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AnimeService: IAnimeService
    {
        private ISettingsService Settings { get; }

        public AnimeService(ISettingsService settings)
        {
            Settings = settings;
        }

        public IEnumerable<Anime> Animes => Settings.Animes;

        public IEnumerable<Anime> FullyWatched()
        {
            return AiringAndWatching
                .Where(
                    anime =>
                        anime.MyAnimeList.HasId &&
                        (anime.MyAnimeList.OverallTotal > 0 && anime.Episode == anime.MyAnimeList.OverallTotal ||
                         anime.MyAnimeList.TotalEpisodes > 0 && anime.Episode == anime.MyAnimeList.TotalEpisodes));
        }

        public IEnumerable<Anime> FilteredAndSorted()
        {
            var animes = Animes;

            var propertyDescriptor = TypeDescriptor
                .GetProperties(typeof(Anime))
                .Find(Settings.SortBy, true);

            if (!string.IsNullOrEmpty(Settings.FilterBy))
            {
                if (Settings.FilterBy.Equals("Needs Synchronize"))
                    animes = animes.Where(anime => anime.MyAnimeList.HasId && anime.MyAnimeList.NeedsUpdating);
                else
                {
                    var filters = Settings.FilterBy.Split('/');
                    animes = animes.Where(a => filters.Any(f => f.Equals(a.Status.Description())));
                }
            }

            return Settings.FlagConfig.SortByReversed
                ? animes.OrderByDescending(x => propertyDescriptor.GetValue(x))
                : animes.OrderBy(x => propertyDescriptor.GetValue(x));
        }

        public IEnumerable<Anime> AiringAndWatchingAndNotCompleted()
        {
            return AiringAndWatching
                .Where(anime =>
                {
                    if (anime.MyAnimeList.HasId && anime.MyAnimeList.Total != 0)
                        return anime.Episode != anime.MyAnimeList.Total;
                    return true;
                });
        }

        public IEnumerable<Anime> AiringAndWatching => Watching.Where(a => a.Airing);

        public IEnumerable<Anime> Watching => Animes.Where(a => a.Status == Status.Watching);

        public IEnumerable<Anime> NeedsUpdates => Animes.Where(a => a.MyAnimeList.NeedsUpdating 
                                                                    && a.Status != Status.Considering);

        public Anime ClosestAnime(string name)
        {
            return Animes
                .Select(a => new StringDistance<Anime>(a, name, a.Name))
                .Where(ap => ap.Distance <= 10)
                .OrderBy(ap => ap.Distance)
                .FirstOrDefault()?.Item;
        }

        public void Add(Anime anime)
        {
            Settings.Animes.Add(anime);
            Settings.Save();
        }

        public void Remove(Anime anime)
        {
            Settings.Animes.Remove(anime);
            Settings.Save();
        }

        public void Remove(string name)
        {
            var anime = Animes.First(a => a.Name.ToLower().Equals(name.ToLower()));
            if (anime != null)
                Remove(anime);
        }

        
    }
}