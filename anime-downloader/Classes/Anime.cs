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

                if (HasMyAnimelistId)
                {
                    MyAnimelistNeedsUpdating = true;
                    if (!MyAnimelistSeriesContinuationEpisode.Equals(""))
                        MyAnimelistSeriesContinuationEpisode =
                            $"{int.Parse(value) - int.Parse(MyAnimelistTotalEpisodes):D2}";
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
                if (HasMyAnimelistId)
                    MyAnimelistNeedsUpdating = true;
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
                if (HasMyAnimelistId)
                    MyAnimelistNeedsUpdating = true;
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
                return 13*SortedRateFlag - 2;
            }
        }

        /* MyAnimeList */

        public bool HasMyAnimelistId => !MyAnimelistId.Equals("");

        public string MyAnimelistId
        {
            get { return Root.Element("myanimelist")?.Element("id")?.Value; }
            set
            {
                Root.Element("myanimelist")?.Element("id")?.SetValue(value);
                Save();
            }
        }

        public string MyAnimelistSynopsis
        {
            get { return Root.Element("myanimelist")?.Element("synopsis")?.Value; }
            set
            {
                Root.Element("myanimelist")?.Element("synopsis")?.SetValue(value);
                Save();
            }
        }

        public string MyAnimelistImage
        {
            get { return Root.Element("myanimelist")?.Element("image")?.Value; }
            set
            {
                Root.Element("myanimelist")?.Element("image")?.SetValue(value);
                Save();
            }
        }

        public string MyAnimelistTitle
        {
            get { return Root.Element("myanimelist")?.Element("title")?.Value; }
            set
            {
                Root.Element("myanimelist")?.Element("title")?.SetValue(value);
                Save();
            }
        }

        public string MyAnimelistEnglish
        {
            get { return Root.Element("myanimelist")?.Element("english")?.Value; }
            set
            {
                Root.Element("myanimelist")?.Element("english")?.SetValue(value);
                Save();
            }
        }

        public bool MyAnimelistNeedsUpdating
        {
            get { return bool.Parse(Root.Element("myanimelist")?.Element("needs-updating")?.Value ?? bool.FalseString); }
            set
            {
                Root.Element("myanimelist")?.Element("needs-updating")?.SetValue(value);
                Save();
            }
        }

        public string MyAnimelistTotalEpisodes
        {
            get { return Root.Element("myanimelist")?.Element("total-episodes")?.Value; }
            set
            {
                Root.Element("myanimelist")?.Element("total-episodes")?.SetValue(value);
                Save();
            }
        }

        public string MyAnimelistSeriesContinuationEpisode
        {
            get { return Root.Element("myanimelist")?.Element("series-continuation-episode")?.Value; }
            set
            {
                Root.Element("myanimelist")?.Element("series-continuation-episode")?.SetValue(value);
                Save();
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
                .Select(e => new {Name = e, Distance = Methods.LevenshteinDistance(Name, e)})
                .OrderBy(e => e.Distance)
                .First()
                .Name;

            return episodes.Where(e => e.Name.Equals(name));
        }

        /// <summary>
        ///     Gets the best guess to what the anime is based solely on name.
        /// </summary>
        public static Anime ClosestTo(IEnumerable<Anime> animes, string name)
        {
            return animes
                .Select(a => new {Anime = a, Distance = Methods.LevenshteinDistance(a.Name, name)})
                .OrderBy(ap => ap.Distance)
                .First()
                .Anime;
        }

        public static Anime ClosestTo(IEnumerable<Anime> animes, AnimeEpisode anime)
        {
            return animes
                .Select(a => new { Anime = a, Distance = Methods.LevenshteinDistance(a.Name, anime.Name) })
                .Where(ap => ap.Distance <= 20)
                .OrderBy(ap => ap.Distance)
                .FirstOrDefault()?
                .Anime;
        }

        public static Anime ClosestTo(Settings settings, string name)
        {
            var animes = new AnimeCollection(settings).Animes;
            return animes
                .Select(a => new {Anime = a, Distance = Methods.LevenshteinDistance(a.Name, name)})
                .OrderBy(ap => ap.Distance)
                .First()
                .Anime;
        }

        public static AnimeEpisode ClosestTo(IEnumerable<AnimeEpisode> animeEpisodes, string name)
        {
            return animeEpisodes
                .Select(a => new {Anime = a, Distance = Methods.LevenshteinDistance(a.Name, name)})
                .Where(ap => ap.Distance <= 15)
                .OrderBy(ap => ap.Distance)
                .First()
                .Anime;
        }

        public XElement ClosestMyAnimelistNode(IEnumerable<XElement> animeNodes)
        {
            return animeNodes
                .Where(n =>
                {
                    var status = n.Element("status")?.Value;
                    if (Airing && status != null)
                        return status.Equals("Currently Airing");
                    return true;
                })
                .Where(n =>
                {
                    if (!NameStrict)
                        return true;
                    var title = n.Element("title")?.Value.ToLower();
                    var english = n.Element("english")?.Value.ToLower();
                    var name = Name.ToLower();
                    return name.Equals(title) || name.Equals(english);
                })
                .Select(n => new MyAnimeListNodeDistance(n, Name, n.Element("title")?.Value, n.Element("english")?.Value))
                // .Where(n => n.Distance <= 80)
                .OrderBy(n => n.Distance)
                .Select(n => n.Node)
                .FirstOrDefault();
        }

        public XElement ClosestMyAnimelistNode(IEnumerable<XElement> animeNodes, XElement filterNode)
        {
            return animeNodes
                .Where(n =>
                {
                    var status = n.Element("status")?.Value;
                    if (Airing && status != null)
                        return status.Equals("Currently Airing");
                    return true;
                })
                .Where(n =>
                {
                    if (!NameStrict)
                        return true;
                    var title = n.Element("title")?.Value.ToLower();
                    var english = n.Element("english")?.Value.ToLower();
                    var name = Name.ToLower();
                    return name.Equals(title) || name.Equals(english);
                })
                .Select(n => new MyAnimeListNodeDistance(n, Name, n.Element("title")?.Value, n.Element("english")?.Value))
                // .Where(n => n.Distance <= 80)
                .OrderBy(n => n.Distance)
                .Select(n => n.Node)
                .FirstOrDefault(n => !(n.Element("id").Value.Equals(filterNode.Element("id").Value)));
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
            string[] separators = {string.Join("+", Title.Replace("'s", "").Split(' ')), episode, Resolution};
            return string.Join("+", separators);
        }

        public string ToRss()
        {
            string[] separators = {string.Join("+", Title.Replace("'s", "").Split(' ')), NextEpisode(), Resolution};
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
    }
}