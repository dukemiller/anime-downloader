using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using anime_downloader.Classes.Xml;
using HtmlAgilityPack;

namespace anime_downloader.Classes {
    public class Anime {
        private readonly XmlController _xml;

        public XContainer Root { get; }
       
        public Anime() {
            Root = new XElement("show",
                new XElement("name"),
                new XElement("episode", "00"),
                new XElement("status", "Watching"),
                new XElement("resolution", "720"),
                new XElement("airing", false),
                new XElement("updated", false),
                new XElement("name-strict", false),
                new XElement("preferredSubgroup"),
                new XElement("last-downloaded", "2016-02-04")
            );
        }
        
        public Anime(XContainer root, XmlController xml) {
            _xml = xml;
            Root = root;
        }

        private void Save() {
            if (_xml == null || !_xml.AutoSave)
                return;
            _xml.SaveAnime();
        }

        public void Remove() {
            _xml?.Remove(this);
        }

        /// <summary>
        ///     Main referenced title.
        /// </summary>
        public string Name {
            get { return Root.Element("name")?.Value; }
            set {
                if (value.Equals(Name))
                    return;
                Root.Element("name")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     User's current watched episode.
        /// </summary>
        public string Episode {
            get { return Root.Element("episode")?.Value; }
            set {
                if (value.Equals(Episode))
                    return;
                Root.Element("episode")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     User's status on watching the anime.
        /// </summary>
        public string Status {
            get { return Root.Element("status")?.Value; }
            set {
                if (value.Equals(Status))
                    return;
                Root.Element("status")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     The quality to be downloaded.
        /// </summary>
        public string Resolution {
            get { return Root.Element("resolution")?.Value; }
            set {
                if (value.Equals(Resolution))
                    return;
                Root.Element("resolution")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     If the anime is ongoing and currently airing.
        /// </summary>
        public bool Airing {
            get { return bool.Parse(Root.Element("airing")?.Value ?? bool.FalseString); }
            set {
                if (value == Airing)
                    return;
                Root.Element("airing")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     if searching for the anime should contain exclusively it's own name with no fragments.
        /// </summary>
        public bool NameStrict {
            get { return bool.Parse(Root.Element("name-strict")?.Value ?? bool.FalseString); }
            set {
                if (value == NameStrict)
                    return;
                Root.Element("name-strict")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     If searching for the anime should only download from a specific subgroup if chosen
        /// </summary>
        public string PreferredSubgroup {
            get { return Root.Element("preferredSubgroup")?.Value; }
            set {
                if (value.Equals(PreferredSubgroup))
                    return;
                Root.Element("preferredSubgroup")?.SetValue(value);
                Save();
            }
        }

        /// <summary>
        ///     Proper title name of anime.
        /// </summary>
        /// <returns>A title</returns>
        public string Title => new CultureInfo("en-US", false).TextInfo.ToTitleCase(Name);

        /// <summary>
        ///     A zero padded string of the number of the next episode.
        /// </summary>
        /// <returns>A padded string representation of the next episode in sequence.</returns>
        public string NextEpisode() => $"{int.Parse(Episode) + 1:D2}";

        /// <summary>
        ///     Joins properties of anime together to a string that can be read by an RSS query.
        /// </summary>
        /// <returns>A RSS parsable string.</returns>
        public string ToRss() {
            string[] seperators = {string.Join("+", Title.Split(' ')), NextEpisode(), Resolution};
            return string.Join("+", seperators);
        }

        /// <summary>
        ///     Seeks the next episode for the current anime on Nyaa.eu
        /// </summary>
        /// <returns>A Nyaa object containing information about the file download.</returns>
        public async Task<IEnumerable<Nyaa>> GetLinksToNextEpisode() {
            var url = new Uri("https://www.nyaa.se/?page=rss&cats=1_37&term=" + ToRss() + "&sort=2");
            var client = new WebClient();
            var document = new HtmlDocument();
            var html = await Task.Run(() => client.DownloadString(url));
            document.LoadHtml(html);
            var itemNodes = document.DocumentNode.SelectNodes("//item");

            var result = itemNodes?.Select(n => new Nyaa(n))
                .Where(n => n.Measurement.Equals("MiB") &&
                            n.Size > 10 &&
                            n.StrippedName().Contains(NextEpisode()) &&
                            n.Seeders > 0);

            return result;
        }
        
    }
}