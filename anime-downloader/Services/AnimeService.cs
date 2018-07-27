using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AnimeService : IAnimeService
    {
        private readonly IAnimeRepository _animeRepository;

        private readonly ISettingsRepository _settingsRepository;

        public AnimeService(ISettingsRepository settingsRepository, IAnimeRepository animeRepository)
        {
            _settingsRepository = settingsRepository;
            _animeRepository = animeRepository;
        }

        public IEnumerable<Anime> Watching => Animes.Where(a => a.Status == Status.Watching);

        public IEnumerable<Anime> Animes => _animeRepository.Animes;

        public IEnumerable<Anime> FullyWatched()
        {
            return Watching
                .Where(
                    anime =>
                        (anime.Details.HasId  || anime.Details.OverallTotal > 0 || anime.Details.TotalEpisodes > 0) &&
                        (anime.Details.OverallTotal > 0 && anime.Episode == anime.Details.OverallTotal ||
                         anime.Details.TotalEpisodes > 0 && anime.Episode == anime.Details.TotalEpisodes));
        }

        public IEnumerable<Anime> FilteredAndSorted()
        {
            var animes = Animes;

            var propertyDescriptor = TypeDescriptor
                .GetProperties(typeof(Anime))
                .Find(_settingsRepository.SortBy, true);

            // Filtering
            if (!string.IsNullOrEmpty(_settingsRepository.FilterBy))
                if (_settingsRepository.FilterBy.Equals("Needs Synchronize"))
                    animes = animes.Where(anime => anime.Details.HasId && anime.Details.NeedsUpdating);
                else if (_settingsRepository.FilterBy.Equals("Current Season"))
                    animes = animes.Where(anime => (anime.Status == Status.Watching || anime.Status == Status.Finished) && anime.Details.HasId && anime.Details.AiringNow);
                else
                {
                    var filters = _settingsRepository.FilterBy.Split('/');
                    animes = animes.Where(a => filters.Any(f => f.Equals(a.Status.Description())));
                }

            // Ordering
            return _settingsRepository.FlagConfig.SortByReversed
                ? animes.OrderByDescending(x => propertyDescriptor?.GetValue(x))
                : animes.OrderBy(x => propertyDescriptor?.GetValue(x));
        }

        public IEnumerable<Anime> AiringAndWatchingAndNotCompleted()
        {
            return AiringAndWatching
                .Where(anime =>
                {
                    if (anime.Details.HasId && anime.Details.Total != 0)
                        return anime.Episode != anime.Details.Total;
                    return true;
                });
        }

        public IEnumerable<Anime> AiringAndWatching => Watching.Where(a => a.Airing);

        public IEnumerable<Anime> NeedsUpdates => Animes.Where(a => a.Details.NeedsUpdating
                                                                    && a.Status != Status.Considering);

        public IEnumerable<Anime> HasId => Animes.Where(a => !string.IsNullOrEmpty(a.Details.Id));

        public bool Synced => Animes.Any() && !NeedsUpdates.Any();

        public bool WatchingAndAiringContains(string name)
        {
            return Animes
                .Where(a => a.Details.AiringNow || a.Details.Aired == AnimeSeason.Current || a.Details.Ended == AnimeSeason.Current)
                .Select(a =>
                {
                    var container = new List<double> {new StringDistance<Anime>(a, name, a.Name).Distance};
                    
                    if (a.Details.TitleAndEnglish.Any())
                        container.Add(a.Details
                            .TitleAndEnglish
                            .Select(s => new StringDistance<string>(s, name, s).Distance)
                            .Min());

                    if (a.Details.SynonymsSplit.Any())
                        container.Add(a.Details
                            .SynonymsSplit
                            .Select(s => new StringDistance<string>(s, name, s).Distance)
                            .Min());

                    return container.Min();
                })
                .Any(ap => ap <= 1);
        }

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
            _animeRepository.Animes.Add(anime);
            _animeRepository.Save();
        }

        public void Remove(Anime anime)
        {
            _animeRepository.Animes.Remove(anime);
            _animeRepository.Save();
        }

        public void Remove(string name)
        {
            var anime = Animes.First(a => a.Name.ToLower().Equals(name.ToLower()));
            if (anime != null)
                Remove(anime);
        }
    }
}