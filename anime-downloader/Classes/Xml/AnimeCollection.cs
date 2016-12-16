using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using anime_downloader.Models;

namespace anime_downloader.Classes.Xml
{
    /// <summary>
    ///     The purpose of this class is to delegate operations that may require
    ///     both read/write operations on the XML files to one location.
    /// </summary>
    public class AnimeCollection
    {
        private readonly Settings _settings;

        public AnimeCollection(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        ///     Retrieve a collection of the anime currently in the anime xml as Anime objects.
        /// </summary>
        public IEnumerable<Anime> Animes => _settings.AnimeDocument.Root?.Elements().Select(a => new Anime(a, _settings));

        /// <summary>
        ///     Retrieve settings-defined filtered and sorted collection of the anime
        ///     currently in the anime xml as Anime objects.
        /// </summary>
        public IEnumerable<Anime> FilteredAndSorted
        {
            get
            {
                var filters = _settings.FilterBy.Split('/');

                var propertyDescriptor = TypeDescriptor
                    .GetProperties(typeof(Anime))
                    .Find(_settings.SortBy, true);

                var animes = Animes;

                if (!_settings.FilterBy.IsBlank())
                    animes = animes.Where(a => filters.Any(f => f.Equals(a.Status)));

                return _settings.Flags.SortByReversed
                    ? animes.OrderByDescending(x => propertyDescriptor.GetValue(x))
                    : animes.OrderBy(x => propertyDescriptor.GetValue(x));
            }
        }

        /// <summary>
        ///     Retrieve collection of the anime that is both airing and being watched from the
        ///     anime xml as Anime objects.
        /// </summary>
        public IEnumerable<Anime> AiringAndWatching => Animes.Where(a => a.Airing && a.Status == "Watching");

        public IEnumerable<Anime> Watching => Animes.Where(a => a.Status == "Watching");

        public IEnumerable<Anime> NeedsUpdates => Animes.Where(a => a.MyAnimeList.NeedsUpdating && !a.Status.Equals("Considering"));

        /// <summary>
        ///     A flag that will save any change in the xml schema as soon as it happens.
        /// </summary>
        public static bool AutoSave { get; set; } = true;

        /// <summary>
        ///     Add an anime instance to the current anime xml.
        /// </summary>
        public void Add(Anime anime)
        {
            _settings.AnimeDocument.Root?.Add(anime.Root);
            if (AutoSave)
                SaveAnime(_settings.AnimeDocument);
        }

        /// <summary>
        ///     Remove an anime from the current anime xml.
        /// </summary>
        public void Remove(Anime anime)
        {
            _settings.AnimeDocument.Root?.Elements().FirstOrDefault(a => a.Element("name")?.Value.Equals(anime.Name) ?? false)?.Remove();
            if (AutoSave)
                SaveAnime(_settings.AnimeDocument);
        }

        /// <summary>
        ///     Explicitly save the anime xml.
        /// </summary>
        public static void SaveAnime(XDocument animeDocument)
        {
            if (!AutoSave)
                return;
            animeDocument.Save(Settings.AnimeXml);
        }

        /// <summary>
        ///     Explicitly save the settings xml.
        /// </summary>
        public static void SaveSettings(XDocument settingsDocument)
        {
            if (!AutoSave)
                return;
            settingsDocument.Save(Settings.SettingsXml);
        }
    }
}