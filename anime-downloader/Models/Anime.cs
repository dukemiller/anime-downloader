using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using anime_downloader.Annotations;
using anime_downloader.Classes;
using anime_downloader.Classes.Distances;
using anime_downloader.Classes.File;
using anime_downloader.Classes.Xml;
using anime_downloader.Enums;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Models
{
    public class Anime
    {
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
            MyAnimeList = new MyAnimeListDetails();
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
            MyAnimeList = new MyAnimeListDetails();
        }

#region -- XML Related

        public XContainer Root { get; }

        /// <summary>
        ///     A variable used sort of like a bit flag for sorting in the data grid.
        /// </summary>
        public static int SortedRateFlag;

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
                            $"{int.Parse(value) - MyAnimeList.TotalEpisodes:D2}";
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

        public bool HasRating => !Rating.Equals("");

        /// <summary>
        ///     Returns rating if able to retrieve, else -1
        /// </summary>
        public int IntRating()
        {
            int rating;
            var successful = int.TryParse(Rating, out rating);
            return successful ? rating : -1;
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

        public MyAnimeListDetails MyAnimeList { get; set; }

        public string EpisodeTotal
        {
            get
            {
                // If there's data about the episode number, retrieve it
                if (MyAnimeList.HasId)
                {
                    // If it has an overall total, this was a mislabeled show and this
                    // needs to be preferred first
                    if (MyAnimeList.OverallTotal > 0)
                        return $"{Episode}/{MyAnimeList.OverallTotal:D2}";

                    // Else just the actual season total if there is one
                    if (MyAnimeList.TotalEpisodes > 0)
                        return $"{Episode}/{MyAnimeList.TotalEpisodes:D2}";
                }
                
                return Episode;
            }
        }

        public string Notes
        {
            get { return Root.Element("notes")?.Value; }
            set
            {
                if (value.Equals(Name))
                    return;
                Root.Element("notes")?.SetValue(value);
                if (MyAnimeList.HasId)
                    MyAnimeList.NeedsUpdating = true;
                Save();
            }
        }

        /* */
        
        /// <summary>
        ///     Returns episode count if able to retrieve, else -1
        /// </summary>
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

        public IEnumerable<AnimeFile> GetEpisodes(Settings settings)
        {
            var episodes = new AnimeFileCollection(settings).GetEpisodes(EpisodeStatus.All).ToList();

            var name = episodes
                .Select(e => e.Name)
                .Distinct()
                .Select(e => new { Name = e, Distance = Methods.LevenshteinDistance(Name, e) })
                .OrderBy(e => e.Distance)
                .First()
                .Name;

            return episodes.Where(e => e.Name.Equals(name));
        }

        public IEnumerable<string> NameCollection
        {
            get
            {
                IEnumerable<string> names;

                if (MyAnimeList.HasId)
                    names = new[] {MyAnimeList.English, MyAnimeList.Title}
                        .Union(MyAnimeList.SynonymsSplit)
                        .SelectMany(c => c.Split())
                        .Distinct();
                else
                    names = Title.Split().Distinct();

                return names.Where(c => c.Length > 0);
            }  
        }

        public AnimeFile LastEpisode => MainWindow.Window.AnimeFileCollection.LastEpisodeOf(this);

        public FindResult ClosestMyAnimeListResult(IEnumerable<FindResult> results)
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
                    if (findResult.TotalEpisodes != 0)
                        return findResult.TotalEpisodes > 2;
                    return true;
                })
                .Select(result => new FindResultDistance(Name, result))
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
        ///     Seeks the next episode for the current anime on Nyaa.eu (according to individual.xml)
        /// </summary>
        /// <returns>
        ///     An enumerable of Nyaa objects containing information about the file downloads.
        /// </returns>
        public async Task<IEnumerable<Torrent>> GetLinksToNextEpisode()
        {
            var result = await Nyaa.GetTorrentsForAsync(this, NextEpisode());
            return result?
                .Select(n => new StringDistance<Torrent>(n, n.StrippedWithNoEpisode, Name))
                .Where(ctd => ctd.Distance <= 25)
                .Select(ctd => ctd.Item);
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
                    .Select(a => new StringDistance<Anime>(a, name, a.Name))
                    .OrderBy(ap => ap.Distance)
                    .FirstOrDefault()?.Item;
            }

            public static Anime To(string name, Settings settings)
            {
                return To(name, new AnimeCollection(settings).Animes);
            }

            public static Anime To(AnimeFile individual, IEnumerable<Anime> animes)
            {
                return animes
                    .Select(a => new StringDistance<Anime>(a, individual.Name, a.Name))
                    .Where(ap => ap.Distance <= 10)
                    .OrderBy(ap => ap.Distance)
                    .FirstOrDefault()?.Item;
            }

            public static Anime To(AnimeFile animeFile, Settings settings)
            {
                return To(animeFile, new AnimeCollection(settings).Animes);
            }

            public static AnimeFile To(string name, IEnumerable<AnimeFile> animeEpisodes)
            {
                return animeEpisodes
                    .Select(a => new { Anime = a, Distance = Methods.LevenshteinDistance(a.Name, name) })
                    .Where(ap => ap.Distance <= 10)
                    .OrderBy(ap => ap.Distance)
                    .FirstOrDefault()?.Anime;
            }
        }
        
    }

}