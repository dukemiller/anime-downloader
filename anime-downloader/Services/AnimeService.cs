using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AnimeService : IAnimeService
    {
        private readonly ISettingsService _settings;

        public AnimeService(ISettingsService settings)
        {
            _settings = settings;
        }

        public IEnumerable<Anime> Watching => Animes.Where(a => a.Status == Status.Watching);

        public IEnumerable<Anime> Animes => _settings.Animes;

        public IEnumerable<Anime> FullyWatched()
        {
            return Watching
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
                .Find(_settings.SortBy, true);

            // Filtering
            if (!string.IsNullOrEmpty(_settings.FilterBy))
                if (_settings.FilterBy.Equals("Needs Synchronize"))
                    animes = animes.Where(anime => anime.MyAnimeList.HasId && anime.MyAnimeList.NeedsUpdating);
                else if (_settings.FilterBy.Equals("Current Season"))
                    animes = animes.Where(anime => (anime.Status == Status.Watching || anime.Status == Status.Finished) && anime.MyAnimeList.HasId && anime.MyAnimeList.AiringNow);
                else
                {
                    var filters = _settings.FilterBy.Split('/');
                    animes = animes.Where(a => filters.Any(f => f.Equals(a.Status.Description())));
                }

            // Ordering
            return _settings.FlagConfig.SortByReversed
                ? animes.OrderByDescending(x => propertyDescriptor?.GetValue(x))
                : animes.OrderBy(x => propertyDescriptor?.GetValue(x));
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

        public IEnumerable<Anime> NeedsUpdates => Animes.Where(a => a.MyAnimeList.NeedsUpdating
                                                                    && a.Status != Status.Considering);

        public IEnumerable<Anime> HasId => Animes.Where(a => !string.IsNullOrEmpty(a.MyAnimeList.Id));

        public bool Synced => Animes.Any() && !NeedsUpdates.Any();

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
            _settings.Animes.Add(anime);
            _settings.Save();
        }

        public void Remove(Anime anime)
        {
            _settings.Animes.Remove(anime);
            _settings.Save();
        }

        public void Remove(string name)
        {
            var anime = Animes.First(a => a.Name.ToLower().Equals(name.ToLower()));
            if (anime != null)
                Remove(anime);
        }
    }
}