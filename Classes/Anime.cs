using HtmlAgilityPack;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace anime_downloader.Classes {

    public class Anime {
        
        /// <summary>
        /// Main referenced title.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// User's current watched episode.
        /// </summary>
        public string episode { get; set; }

        /// <summary>
        /// User's status on watching the anime.
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// The quality to be downloaded.
        /// </summary>
        public string resolution { get; set; }

        /// <summary>
        /// If the anime is ongoing and currently airing.
        /// </summary>
        public bool airing { get; set; }

        /// <summary>
        /// if searching for the anime should contain exclusively it's own name with no fragments.
        /// </summary>
        public bool nameStrict { get; set; }
        
        /// <summary>
        /// For use in property set initializers.
        /// </summary>
        public Anime() {}

        /// <summary>
        /// XML anime initializer.
        /// </summary>
        /// <param name="root">The root node from the anime XML file</param>
        public Anime(XElement root) {
            name = root.Element("name").Value;
            episode = root.Element("episode").Value;
            status = root.Element("status").Value;
            resolution = root.Element("resolution").Value;
            airing = Boolean.Parse(root.Element("airing").Value);
            nameStrict = Boolean.Parse(root.Element("name-strict").Value);
        }

        /// <summary>
        /// Proper title name of anime.
        /// </summary>
        /// <returns>A title</returns>
        public string title() => new CultureInfo("en-US", false).TextInfo.ToTitleCase(name);

        /// <summary>
        /// A zero padded string of the number of the next episode.
        /// </summary>
        /// <returns>A padded string representation of the next episode in sequence.</returns>
        public string nextEpisode() => string.Format("{0:D2}", (int.Parse(episode) + 1));

        /// <summary>
        /// Joins properties of anime together to a string that can be read by an RSS query.
        /// </summary>
        /// <returns>A RSS parsable string.</returns>
        public string toRSS() {
            String[] seperators = {String.Join("+", title().Split(' ')), nextEpisode(), resolution};
            return String.Join("+", seperators);
        }

        /// <summary>
        /// Seeks the next episode for the current anime on Nyaa.eu
        /// </summary>
        /// <returns>A Nyaa object containing information about the file download.</returns>
        public async Task<Nyaa> getLinkToNextEpisode() {
            Uri url = new Uri("https://www.nyaa.eu/?page=rss&cats=1_37&term=" + toRSS() + "&sort=2");
            WebClient client = new WebClient();
            HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            string html = await Task.Run(() => client.DownloadString(url));
            document.LoadHtml(html);
            HtmlNodeCollection itemNodes = document.DocumentNode.SelectNodes("//item");

            if (itemNodes != null) {
                Nyaa[] result = itemNodes.Select(n => new Nyaa(n))
                    .Where(n => n.measurement.Equals("MiB") &&
                                n.size > 10 &&
                                n.strippedName().Contains(nextEpisode()) &&
                                n.seeders > 0)
                    .ToArray();

                foreach (Nyaa nyaa in result) {
                    if (!nameStrict)
                        return nyaa;
                    if (nameStrict && name.Equals(nyaa.strippedName(true)))
                        return nyaa;
                }
            }
            return null;
        }

     }
}
