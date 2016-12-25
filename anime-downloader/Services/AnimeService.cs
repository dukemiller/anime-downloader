using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public IEnumerable<Anime> FilteredAndSorted()
        {
            var propertyDescriptor = TypeDescriptor
                .GetProperties(typeof(Anime))
                .Find(Settings.SortBy, true);

            var animes = Animes;

            if (!string.IsNullOrEmpty(Settings.FilterBy))
            {
                var filters = Settings.FilterBy.Split('/');
                animes = animes.Where(a => filters.Any(f => f.Equals(a.Status)));
            }

            return Settings.FlagConfig.SortByReversed
                ? animes.OrderByDescending(x => propertyDescriptor.GetValue(x))
                : animes.OrderBy(x => propertyDescriptor.GetValue(x));
        }

        public IEnumerable<Anime> AiringAndWatching => Watching.Where(a => a.Airing);

        public IEnumerable<Anime> Watching => Animes.Where(a => a.Status.Equals("Watching"));

        public IEnumerable<Anime> NeedsUpdates => Animes.Where(a => a.MyAnimeList.NeedsUpdating 
                                                                    && !a.Status.Equals("Considering"));

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
    }
}