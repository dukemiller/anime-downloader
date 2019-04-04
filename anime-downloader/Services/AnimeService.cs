using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Classes.Xaml;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Interfaces;
using Optional;
using Optional.Collections;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.Services
{
    public class AnimeService : IAnimeService
    {
        private readonly IAnimeRepository _animeRepository;

        private readonly ISettingsRepository _settingsRepository;

        // 

        public AnimeService(ISettingsRepository settingsRepository, IAnimeRepository animeRepository)
        {
            _settingsRepository = settingsRepository;
            _animeRepository = animeRepository;
        }

        // 

        public IEnumerable<Anime> Animes => _animeRepository.Animes;

        public IEnumerable<Anime> Watching => Animes
            .Where(Predicates.Watching);

        public IEnumerable<Anime> AiringAndWatching => Animes
            .Where(Predicates.Watching)
            .Where(Predicates.MarkedAsAiring);

        public IEnumerable<Anime> NeedsUpdates => Animes
            .Where(Predicates.NeedsUpdates);

        public IEnumerable<Anime> HasMyAnimeListId => Animes
            .Where(Predicates.HasMyAnimeListId);

        public IEnumerable<Anime> AiringAndWatchingAndNotCompleted => Animes
            .Where(Predicates.Watching)
            .Where(Predicates.MarkedAsAiring)
            .Where(Predicates.NotCompleted);

        public IEnumerable<Anime> FullyWatched => Animes
            .Where(Predicates.Watching)
            .Where(Predicates.OnFinalEpisode);

        public IEnumerable<Anime> WatchingOrCompleted => Animes
            .Where(Or(Predicates.Watching, Predicates.Completed));

        public bool Synced => Animes.Any() && !NeedsUpdates.Any();

        public IEnumerable<Anime> FilteredAndSorted()
        {
            var animes = Animes;

            // Filtering
            if (!string.IsNullOrEmpty(_settingsRepository.FilterBy))
                switch (_settingsRepository.FilterBy)
                {
                    case "Needs Synchronize":
                        animes = animes
                            .Where(Predicates.HasMyAnimeListId)
                            .Where(Predicates.NeedsUpdates);
                        break;
                    case "Current Season":
                        animes = animes
                            .Where(Predicates.HasMyAnimeListId)
                            .Where(Predicates.AiringNow)
                            .Where(Predicates.WatchingOrFinished);
                        break;
                    default:
                    {
                        var filters = _settingsRepository.FilterBy.Split('/');
                        animes = animes.Where(anime => filters.Any(filter => filter.Equals(anime.Status.Description())));
                        break;
                    }
                }

            // Ordering
            switch (_settingsRepository.SortBy)
            {
                case "rating":
                    return _settingsRepository.FlagConfig.SortByReversed
                        ? animes.OrderBy(a => a, new RatingSort(ListSortDirection.Ascending))
                        : animes.OrderBy(a => a, new RatingSort(ListSortDirection.Descending));
                case "aired in":
                    return _settingsRepository.FlagConfig.SortByReversed
                        ? animes.OrderBy(a => a, new AiringSort(ListSortDirection.Ascending))
                        : animes.OrderBy(a => a, new AiringSort(ListSortDirection.Descending));
                default:
                    var propertyDescriptor = TypeDescriptor
                        .GetProperties(typeof(Anime))
                        .Find(_settingsRepository.SortBy, true);

                    return _settingsRepository.FlagConfig.SortByReversed
                        ? animes.OrderByDescending(anime => propertyDescriptor?.GetValue(anime))
                        : animes.OrderBy(anime => propertyDescriptor?.GetValue(anime));
            }
        }

        // 

        public bool WatchingAndAiringContains(AiringAnime airing) =>
            Animes.Any(anime => anime.Details.Id == airing.IdMal?.ToString() || anime.Details.AniId == airing.Id);

        public Option<Anime> ClosestAnime(string name) =>
            Animes.Select(anime => new StringDistance<Anime>(anime, name, anime.Name))
                .Where(pair => pair.Distance <= 10)
                .OrderBy(pair => pair.Distance)
                .FirstOrNone()
                .Map(pair => pair.Item);

        // 

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
            => Animes.First(a => a.Name.ToLower().Equals(name.ToLower())).SomeNotNull().MatchSome(Remove);
    }
}