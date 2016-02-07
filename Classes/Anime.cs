using HtmlAgilityPack;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace anime_downloader.Classes {
    public class Anime {

        public Anime() {
            
        }

        public Anime(XElement root) {
            name = root.Element("name").Value;
            episode = root.Element("episode").Value;
            status = root.Element("status").Value;
            resolution = root.Element("resolution").Value;
            airing = Boolean.Parse(root.Element("airing").Value);
            nameStrict = Boolean.Parse(root.Element("name-strict").Value);
            //name = root.Element("updated").Value;
            //name = root.Element("last-downloaded").Value;
        }

        public string title() => new CultureInfo("en-US", false).TextInfo.ToTitleCase(name);

        public string nextEpisode() => string.Format("{0:D2}", (int.Parse(episode) + 1));

        public string toRSS() {
            String[] seperators = {String.Join("+", title().Split(' ')), nextEpisode(), resolution};
            return String.Join("+", seperators);
        }

        public async Task<Nyaa> getLinkToNextEpisode() {
            Uri url = new Uri("https://www.nyaa.eu/?page=rss&cats=1_37&term=" + toRSS() + "&sort=2");
            WebClient client = new WebClient();
            HtmlDocument document = new HtmlAgilityPack.HtmlDocument();

            string html = await Task.Run(() => client.DownloadString(url));
            document.LoadHtml(html);
            HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//item");

            if (nodes != null) {
                Nyaa[] result = nodes.Select(n => new Nyaa(n))
                    .Where(n => n.measurement.Equals("MiB") &&
                                n.size > 10 &&
                                n.name.Contains(nextEpisode()) &&
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

        public async Task<bool> downloadLatestEpisode() {
            Nyaa nyaa = await getLinkToNextEpisode();
            if (nyaa != null) {
                
            }
            return true;
        }

        public string name { get; set; }
        public string episode { get; set; }
        public string status { get; set; }
        public string resolution { get; set; }
        public bool airing { get; set; }
        public bool nameStrict { get; set; }
     }
}
