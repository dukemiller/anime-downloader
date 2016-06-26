using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using anime_downloader.Classes.File;
using anime_downloader.Classes.Web;
using anime_downloader.Classes.Web.MyAnimeList;
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
                Root.Element("episode")?.SetValue(value);

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
            get { return Root.Element("rating")?.Value ?? ""; }
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
                        return $"{Episode}/{MyAnimeList.OverallTotal}";

                    // Else just the actual season total if there is one
                    if (MyAnimeList.IntTotalEpisodes() > 0)
                        return $"{Episode}/{MyAnimeList.TotalEpisodes}";
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

        public IEnumerable<AnimeEpisode> GetEpisodes(Settings settings)
        {
            var episodes = new FileHandler(settings).Episodes(EpisodeType.All).ToList();

            var name = episodes
                .Select(e => e.Name)
                .Distinct()
                .Select(e => new { Name = e, Distance = Methods.LevenshteinDistance(Name, e) })
                .OrderBy(e => e.Distance)
                .First()
                .Name;

            return episodes.Where(e => e.Name.Equals(name));
        }

        public FindResult ClosestMyAnimeListResult(IEnumerable<FindResult> results)
        {
            return results
                .Where(result => !result.Type.Equals("OVA")) // I'm sure i'll regret this
                .Where(result =>        
                {
                    if (!NameStrict)
                        return true;
                    var synonyms = result.Synonyms.Split(';');
                    return new[] {result.Title, result.English}.Union(synonyms).Any(
                            r => r.ToLower().Equals(Name.ToLower()));
                })
                .Where(findResult =>    
                {
                    if (findResult.IntTotalEpisodes() != 0)
                        return findResult.IntTotalEpisodes() > 2;
                    return true;
                })
                .Select(result => new FindResultDistance(Name, result))
                .OrderBy(resultDistance => resultDistance.Distance)
                .FirstOrDefault()?.FindResult;
        }
        
        private void Save()
        {
            if (_settings == null || !AnimeCollection.AutoSave)
                return;
            AnimeCollection.SaveAnime(_settings.AnimeDocument);
        }

        /// <summary>
        ///     A zero padded string of the number of the next episode.
        /// </summary>
        /// <returns>A padded string representation of the next episode in sequence.</returns>
        public string NextEpisode() => $"{IntEpisode() + 1:D2}";

        /// <summary>
        ///     Joins properties of anime together to a string that can be read by an RSS query.
        /// </summary>
        /// <returns>A RSS parsable string.</returns>
        public string ToRss(string episode)
        {
            string[] separators = { string.Join("+", Title.Replace("'s", "").Split(' ')), episode, Resolution };
            return string.Join("+", separators);
        }

        public string ToRss()
        {
            string[] separators = { string.Join("+", Title.Replace("'s", "").Split(' ')), NextEpisode(), Resolution };
            return string.Join("+", separators);
        }

        public async Task<IEnumerable<TorrentProvider>> GetLinksToEpisode(string episode)
        {
            return await Nyaa.GetTorrentsForAsync(this, episode);
        }

        public async Task<IEnumerable<TorrentProvider>> GetLinksToCurrentEpisode()
        {
            return await Nyaa.GetTorrentsForAsync(this, Episode);
        }

        /// <summary>
        ///     Seeks the next episode for the current anime on Nyaa.eu
        /// </summary>
        /// <returns>A Nyaa object containing information about the file download.</returns>
        public async Task<IEnumerable<TorrentProvider>> GetLinksToNextEpisode()
        {
            return await Nyaa.GetTorrentsForAsync(this, NextEpisode());
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
                    .Select(a => new { Anime = a, Distance = Methods.LevenshteinDistance(a.Name, name) })
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
                    .Select(a => new { Anime = a, Distance = Methods.LevenshteinDistance(a.Name, anime.Name) })
                    .Where(ap => ap.Distance <= 20)
                    .OrderBy(ap => ap.Distance)
                    .FirstOrDefault()?.Anime;
            }

            public static AnimeEpisode To(string name, IEnumerable<AnimeEpisode> animeEpisodes)
            {
                return animeEpisodes
                    .Select(a => new { Anime = a, Distance = Methods.LevenshteinDistance(a.Name, name) })
                    .Where(ap => ap.Distance <= 15)
                    .OrderBy(ap => ap.Distance)
                    .FirstOrDefault()?.Anime;
            }
        }

    }

    public class MyAnimeListDetails
    {
        public XElement Root;

        public Action Save;

        public MyAnimeListDetails(XElement root, Action save)
        {
            Root = root;
            Save = save;
        }

        public bool HasId => !Id.Equals("");

        public string Id
        {
            get { return Root.Element("id")?.Value; }
            set
            {
                Root.Element("id")?.SetValue(value);
                Save();
            }
        }

        public string Synopsis
        {
            get { return Root.Element("synopsis")?.Value; }
            set
            {
                Root.Element("synopsis")?.SetValue(value);
                Save();
            }
        }

        public string Image
        {
            get { return Root.Element("image")?.Value; }
            set
            {
                Root.Element("image")?.SetValue(value);
                Save();
            }
        }

        public string Title
        {
            get { return Root.Element("title")?.Value; }
            set
            {
                Root.Element("title")?.SetValue(value);
                Save();
            }
        }

        public string English
        {
            get { return Root.Element("english")?.Value; }
            set
            {
                Root.Element("english")?.SetValue(value);
                Save();
            }
        }

        public string Synonyms
        {
            get { return Root.Element("synonyms")?.Value; }
            set
            {
                Root.Element("synonyms")?.SetValue(value);
                Save();
            }
        }

        public bool NeedsUpdating
        {
            get { return bool.Parse(Root.Element("needs-updating")?.Value ?? bool.FalseString); }
            set
            {
                Root.Element("needs-updating")?.SetValue(value);
                Save();
            }
        }

        public string TotalEpisodes
        {
            get { return Root.Element("total-episodes")?.Value; }
            set
            {
                Root.Element("total-episodes")?.SetValue(value);
                Save();
            }
        }

        public string OverallTotal
        {
            get { return Root.Element("overall-total")?.Value; }
            set
            {
                Root.Element("overall-total")?.SetValue(value);
                Save();
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
            get { return Root.Element("series-continuation-episode")?.Value; }
            set
            {
                Root.Element("series-continuation-episode")?.SetValue(value);
                Save();
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