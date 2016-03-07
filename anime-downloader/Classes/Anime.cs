using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace anime_downloader.Classes {
    public class Anime {
        /// <summary>
        ///     For use in property set initializers.
        /// </summary>
        public Anime() {}

        /// <summary>
        ///     XML anime initializer.
        /// </summary>
        /// <param name="root">The root node from the anime XML file</param>
        public Anime(XElement root) {
            Name = root.Element("name")?.Value;
            Episode = root.Element("episode")?.Value;
            Status = root.Element("status")?.Value;
            Resolution = root.Element("resolution")?.Value;
            Airing = bool.Parse(root.Element("airing")?.Value ?? "false");
            NameStrict = bool.Parse(root.Element("name-strict")?.Value ?? "false");
            PreferredSubgroup = root.Element("preferredSubgroup")?.Value;
        }

        /// <summary>
        ///     Main referenced title.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     User's current watched episode.
        /// </summary>
        public string Episode { get; set; } = "00";

        /// <summary>
        ///     User's status on watching the anime.
        /// </summary>
        public string Status { get; set; } = "Watching";

        /// <summary>
        ///     The quality to be downloaded.
        /// </summary>
        public string Resolution { get; set; } = "720";

        /// <summary>
        ///     If the anime is ongoing and currently airing.
        /// </summary>
        public bool Airing { get; set; }

        /// <summary>
        ///     if searching for the anime should contain exclusively it's own name with no fragments.
        /// </summary>
        public bool NameStrict { get; set; }

        /// <summary>
        ///     If searching for the anime should only download from a specific subgroup if chosen
        /// </summary>
        public string PreferredSubgroup { get; set; } = "";

        /// <summary>
        ///     Proper title name of anime.
        /// </summary>
        /// <returns>A title</returns>
        public string Title() => new CultureInfo("en-US", false).TextInfo.ToTitleCase(Name);

        /// <summary>
        ///     A zero padded string of the number of the next episode.
        /// </summary>
        /// <returns>A padded string representation of the next episode in sequence.</returns>
        public string NextEpisode() => $"{int.Parse(Episode) + 1:D2}";

        /// <summary>
        ///     Joins properties of anime together to a string that can be read by an RSS query.
        /// </summary>
        /// <returns>A RSS parsable string.</returns>
        public string ToRSS() {
            string[] seperators = {string.Join("+", Title().Split(' ')), NextEpisode(), Resolution};
            return string.Join("+", seperators);
        }

        /// <summary>
        ///     Seeks the next episode for the current anime on Nyaa.eu
        /// </summary>
        /// <returns>A Nyaa object containing information about the file download.</returns>
        public async Task<IEnumerable<Nyaa>> GetLinksToNextEpisode() {
            var url = new Uri("https://www.nyaa.se/?page=rss&cats=1_37&term=" + ToRSS() + "&sort=2");
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