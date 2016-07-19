using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using anime_downloader.Classes.File;
using anime_downloader.Classes.Web;
using anime_downloader.Classes.Xml;

namespace anime_downloader.Classes
{
    public class Anime
    {
        /// <summary>
        ///     A variable used sort of like a bit flag for sorting in the data grid.
        /// </summary>
        public static int SortedRateFlag;

        private readonly Settings _settings;

        /// <summary>
        ///     Create an empty anime xml node.
        /// </summary>
        /// <remarks>
        ///     Must be explicitly added to the schema with the AnimeCollection.
        /// </remarks>
        public Anime()
        {
            Root = Schema.AnimeNode();
            MyAnimeList = new MyAnimeListDetails(Root.Element("myanimelist"), Save);
        }

        /// <summary>
        ///     Create an anime object with explicit xml nodes.
        /// </summary>
        /// <remarks>
        ///     Preferably read from the already existing schema and
        ///     instantiated from the AnimeCollection.
        /// </remarks>
        public Anime(XContainer root, Settings settings)
        {
            Root = root;
            _settings = settings;
            MyAnimeList = new MyAnimeListDetails(Root.Element("myanimelist"), Save);
        }

#region -- XML Related

        public XContainer Root { get; }

        /// <summary>
        ///     Main referenced title.
        /// </summary>
        public string Name
        {
            get { return Root.Element("name")?.Value; }
            set
            {
                if (value.Equals(Name))
                    return;
                Root.Element("name")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     User's current watched episode.
        /// </summary>
        public string Episode
        {
            get { return Root.Element("episode")?.Value; }
            set
            {
                if (value.Equals(Episode))
                    return;

                int intValue;
                if (!int.TryParse(value, out intValue) || intValue < 0)
                    return;

                Root.Element("episode")?.SetValue($"{intValue:D2}");

                if (MyAnimeList.HasId)
                {
                    MyAnimeList.NeedsUpdating = true;
                    if (!MyAnimeList.SeriesContinuationEpisode.IsBlank())
                        MyAnimeList.SeriesContinuationEpisode =
                            $"{int.Parse(value) - int.Parse(MyAnimeList.TotalEpisodes):D2}";
                }

                Save();
            }
        }

        /// <summary>
        ///     User's status on watching the anime.
        /// </summary>
        public string Status
        {
            get { return Root.Element("status")?.Value; }
            set
            {
                if (value.Equals(Status))
                    return;
                Root.Element("status")?.SetValue(value);
                if (MyAnimeList.HasId)
                    MyAnimeList.NeedsUpdating = true;
                Save();
            }
        }

        /// <summary>
        ///     The quality to be downloaded.
        /// </summary>
        public string Resolution
        {
            get { return Root.Element("resolution")?.Value; }
            set
            {
                if (value.Equals(Resolution))
                    return;
                Root.Element("resolution")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     If the anime is ongoing and currently airing.
        /// </summary>
        public bool Airing
        {
            get { return bool.Parse(Root.Element("airing")?.Value ?? bool.FalseString); }
            set
            {
                if (value == Airing)
                    return;
                Root.Element("airing")?.SetValue(value);
                Save();
            }
        }

        public string AiringSymbol => Airing ? "✓" : "";

        /// <summary>
        ///     if searching for the anime should contain exclusively it's own name with no fragments.
        /// </summary>
        public bool NameStrict
        {
            get { return bool.Parse(Root.Element("name-strict")?.Value ?? bool.FalseString); }
            set
            {
                if (value == NameStrict)
                    return;
                Root.Element("name-strict")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     If searching for the anime should only download from a specific subgroup if chosen
        /// </summary>
        public string PreferredSubgroup
        {
            get { return Root.Element("preferredSubgroup")?.Value; }
            set
            {
                if (value.Equals(PreferredSubgroup))
                    return;
                Root.Element("preferredSubgroup")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The personal rating given for the series.
        /// </summary>
        public string Rating
        {
            get { return Root.Element("rating")?.Value; }
            set
            {
                if (value.Equals(Rating))
                    return;
                if (!value.All(char.IsNumber) && !value.Equals(""))
                    return;
                Root.Element("rating")?.SetValue(value);
                if (MyAnimeList.HasId)
                    MyAnimeList.NeedsUpdating = true;
                Save();
            }
        }

        /// <summary>
        ///     A property used for sorting the rating in the datagrid
        /// </summary>
        public int SortedRating
        {
            get
            {
                int val;
                if (int.TryParse(Rating, out val))
                    return val;
                return 13 * SortedRateFlag - 2;
            }
        }

        public MyAnimeListDetails MyAnimeList { get; }

        public string EpisodeTotal
        {
            get
            {
                // If there's data about the episode number, retrieve it
                if (MyAnimeList.HasId)
                {
                    // If it has an overall total, this was a mislabeled show and this
                    // needs to be preferred first
                    if (MyAnimeList.IntOverallTotal() > 0)
                        return $"{Episode}/{MyAnimeList.IntOverallTotal():D2}";

                    // Else just the actual season total if there is one
                    if (MyAnimeList.IntTotalEpisodes() > 0)
                        return $"{Episode}/{MyAnimeList.IntTotalEpisodes():D2}";
                }
                
                return Episode;
            }
        }

        /* */
        
        public int IntEpisode()
        {
            int episode;
            var successful = int.TryParse(Episode, out episode);
            return successful ? episode : -1;
        }

        /// <summary>
        ///     Proper title name of anime.
        /// </summary>
        /// <returns>A title</returns>
        public string Title => new CultureInfo("en-US", false).TextInfo.ToTitleCase(Name);

        /* */

        private void Save()
        {
            if (_settings == null || !AnimeCollection.AutoSave)
                return;
            AnimeCollection.SaveAnime(_settings.AnimeDocument);
        }

#endregion

        public IEnumerable<AnimeEpisode> GetEpisodes(Settings settings)
        {
            var episodes = new FileHandler(settings).Episodes(EpisodeType.All);

            var name = episodes
                .Select(e => e.Name)
                .Distinct()
                .Select(e => new { Name = e, Distance = Methods.LevenshteinDistance(Name, e) })
                .OrderBy(e => e.Distance)
                .First()
                .Name;

            return episodes.Where(e => e.Name.Equals(name));
        }

        public Web.MyAnimeList.FindResult ClosestMyAnimeListResult(IEnumerable<Web.MyAnimeList.FindResult> results)
        {
            var closestResults = results
                .Where(result => !result.Type.Equals("OVA")) // I'm sure i'll regret this
                .Where(result =>
                {
                    if (!NameStrict)
                        return true;
                    return result.NameCollection.Any(r => r.ToLower().Replace(" (tv)", "").Equals(Name.ToLower()));
                })
                .Where(findResult =>
                {
                    if (findResult.IntTotalEpisodes() != 0)
                        return findResult.IntTotalEpisodes() > 2;
                    return true;
                })
                .Select(result => new Web.MyAnimeList.FindResultDistance(Name, result))
                .OrderBy(resultDistance => resultDistance.Distance);

            var closest = closestResults.FirstOrDefault();

            // if any values have the same exact distance
            if (closestResults.Any(c => closest?.Distance == c.Distance))
            {
                // get the most recently airing show
                closest = closestResults.Where(c => c.Distance == closest?.Distance)
                    .OrderByDescending(r => DateTime.Parse(r.FindResult.StartDate))
                    .FirstOrDefault();
            }

            return closest?.FindResult;
        }

        /// <summary>
        ///     A zero padded string of the number of the next episode.
        /// </summary>
        /// <returns>A padded string representation of the next episode in sequence.</returns>
        public string NextEpisode() => $"{IntEpisode() + 1:D2}";
        
        /// <summary>
        ///     Seeks the next episode for the current anime on Nyaa.eu (according to anime.xml)
        /// </summary>
        /// <returns>
        ///     An enumerable of Nyaa objects containing information about the file downloads.
        /// </returns>
        public async Task<IEnumerable<TorrentProvider>> GetLinksToNextEpisode()
        {
            var result = await Nyaa.GetTorrentsForAsync(this, NextEpisode());
            return result?
                .Select(n => new ClosestTorrentDistance(n, this))
                .Where(ctd => ctd.Distance <= 20)
                .Select(ctd => ctd.TorrentProvider);
        }

        /// <summary>
        ///     A set of static retrieval methods for finding anime in the collection without needing to strum up
        ///     linq methods, getting the best guess to what the anime is based solely on the given input string
        /// </summary>
        public static class Closest
        {
            public static Anime To(string name, IEnumerable<Anime> animes)
            {
                return animes
                    .Select(a => new ClosestStringDistance(name, a))
                    .OrderBy(ap => ap.Distance)
                    .FirstOrDefault()?.Anime;
            }

            public static Anime To(string name, Settings settings)
            {
                return To(name, new AnimeCollection(settings).Animes);
            }

            public static Anime To(AnimeEpisode anime, IEnumerable<Anime> animes)
            {
                return animes
                    .Select(a => new ClosestStringDistance(anime.Name, a))
                    .Where(ap => ap.Distance <= 10)
                    .OrderBy(ap => ap.Distance)
                    .FirstOrDefault()?.Anime;
            }

            public static Anime To(AnimeEpisode animeEpisode, Settings settings)
            {
                return To(animeEpisode, new AnimeCollection(settings).Animes);
            }

            public static AnimeEpisode To(string name, IEnumerable<AnimeEpisode> animeEpisodes)
            {
                return animeEpisodes
                    .Select(a => new { Anime = a, Distance = Methods.LevenshteinDistance(a.Name, name) })
                    .Where(ap => ap.Distance <= 10)
                    .OrderBy(ap => ap.Distance)
                    .FirstOrDefault()?.Anime;
            }
        }

        private class ClosestStringDistance
        {
            private readonly double _relevance;

            private readonly double _distance;

            public double Distance => _distance * (2 - _relevance);

            public Anime Anime { get; }

            public ClosestStringDistance(string animeName, Anime comparison)
            {
                var namesplit = animeName.ToLower().Trim().Split(' ').Distinct().ToArray();
                var groupsplit = comparison.Name.ToLower().Trim().Split(' ').Distinct().ToArray();
                _distance = Methods.LevenshteinDistance(animeName, comparison.Name);
                _relevance = (double) groupsplit.Count(a => namesplit.Contains(a)) / groupsplit.Length;
                Anime = comparison;
            }
        }

        private class ClosestTorrentDistance
        {
            private readonly double _relevance;

            private readonly double _distance;

            public double Distance => _distance * (2 - _relevance);

            public TorrentProvider TorrentProvider { get; }

            public ClosestTorrentDistance(TorrentProvider nyaa, Anime comparison)
            {
                var animeName = nyaa.StrippedName(true);
                var namesplit = animeName.ToLower().Trim().Split(' ').Distinct().ToArray();
                var groupsplit = comparison.Name.ToLower().Trim().Split(' ').Distinct().ToArray();
                _distance = Methods.LevenshteinDistance(animeName, comparison.Name);
                _relevance = (double) groupsplit.Count(a => namesplit.Contains(a)) / groupsplit.Length;
                TorrentProvider = nyaa;
                
                /*
                Console.WriteLine($"'{animeName}' COMPARED TO '{comparison.Name}'");
                Console.WriteLine($"-- {_distance}, {_relevance}, {Distance}");
                Console.WriteLine($"-- {namesplit.CommaJoined()} || {groupsplit.CommaJoined()}");
                */
            }
        }
    }

    public class MyAnimeListDetails
    {
        private readonly XElement _root;

        private readonly Action _save;

        public MyAnimeListDetails(XElement root, Action save)
        {
            _root = root;
            _save = save;
        }

        public bool HasId => !Id.Equals("");

        public string Id
        {
            get { return _root.Element("id")?.Value; }
            set
            {
                _root.Element("id")?.SetValue(value);
                _save();
            }
        }

        public string Synopsis
        {
            get { return _root.Element("synopsis")?.Value; }
            set
            {
                _root.Element("synopsis")?.SetValue(value);
                _save();
            }
        }

        public string Image
        {
            get { return _root.Element("image")?.Value; }
            set
            {
                _root.Element("image")?.SetValue(value);
                _save();
            }
        }

        public string Title
        {
            get { return _root.Element("title")?.Value; }
            set
            {
                _root.Element("title")?.SetValue(value);
                _save();
            }
        }

        public string English
        {
            get { return _root.Element("english")?.Value; }
            set
            {
                _root.Element("english")?.SetValue(value);
                _save();
            }
        }

        public string Synonyms
        {
            get { return _root.Element("synonyms")?.Value; }
            set
            {
                _root.Element("synonyms")?.SetValue(value);
                _save();
            }
        }

        private IEnumerable<string> SynonymsSplit => Synonyms.Split(';');

        public IEnumerable<string> NameCollection => new[] {English, Title}.Union(SynonymsSplit);

        public bool NeedsUpdating
        {
            get { return bool.Parse(_root.Element("needs-updating")?.Value ?? bool.FalseString); }
            set
            {
                _root.Element("needs-updating")?.SetValue(value);
                _save();
            }
        }

        public string TotalEpisodes
        {
            get { return _root.Element("total-episodes")?.Value; }
            set
            {
                _root.Element("total-episodes")?.SetValue(value);
                _save();
            }
        }

        public string OverallTotal
        {
            get { return _root.Element("overall-total")?.Value; }
            set
            {
                _root.Element("overall-total")?.SetValue(value);
                _save();
            }
        }

        public int IntOverallTotal() {
            int episodes;
            var successful = int.TryParse(OverallTotal, out episodes);
            return successful ? episodes : 0;
        }

        public int IntTotalEpisodes()
        {
            int episodes;
            var successful = int.TryParse(TotalEpisodes, out episodes);
            return successful ? episodes : 0;
        }

        public string SeriesContinuationEpisode
        {
            get { return _root.Element("series-continuation-episode")?.Value; }
            set
            {
                _root.Element("series-continuation-episode")?.SetValue(value);
                _save();
            }
        }

        public int IntSeriesContinuationEpisode()
        {
            int episodes;
            var successful = int.TryParse(SeriesContinuationEpisode, out episodes);
            return successful ? episodes : 0;
        }

    }

}