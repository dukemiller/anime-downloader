using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace anime_downloader.Classes.Web
{
    public class Nyaa : Torrent
    {
        /// <summary>
        ///     A conversion chart from any of these values to megabytes.
        /// </summary>
        private static readonly Dictionary<string, double> ToMegabyte = new Dictionary<string, double>
        {
            {"MiB", 1.04858},
            {"GiB", 1073.74},
            {"KiB", 0.001024}
        };

        private const string EnglishTranslated = "1_37";

        private const string BySeeders = "2";

        /// <summary>
        ///     HTML Nyaa Initializer
        /// </summary>
        public Nyaa(HtmlNode node)
        {
            Name = WebUtility.HtmlDecode(node.Element("title").InnerText.Replace("Â", ""));
            Link = node.Element("#text").InnerText.Replace("#38;", "");
            Description = node.Element("description").InnerText;
            if (Description.Contains("CDATA"))
                Description = Description
                    .Split(new[] { "<![CDATA[" }, StringSplitOptions.None)[1]
                    .Split(new[] { "]]>" }, StringSplitOptions.None)[0];
            Seeders = int.Parse(Description.Split(new[] { " seeder" }, StringSplitOptions.None)[0]);
            Measurement = ToMegabyte.First(d => Description.Contains(d.Key)).Key;
            Size = Math.Round(double.Parse(Description.Split(new[] { $" {Measurement}" }, StringSplitOptions.None)[0]
                .Split(new[] { " - " }, StringSplitOptions.None)[1]) * ToMegabyte[Measurement], 2);
        }

        /// <summary>
        ///     Check if Nyaa.se is online within 3.0 seconds so not to hang when entering download view.
        /// </summary>
        public static async Task<bool> IsOnlineAsync()
        {
            try
            {
                const string link = "https://www.nyaa.se/";

                var request = (HttpWebRequest) WebRequest.Create(link);
                request.Timeout = 3000;
                request.AllowAutoRedirect = false;
                request.Method = "HEAD";

                using (var response = await request.GetResponseAsync())
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        ///     Get torrents that qualify as downloadable (according to settings.xml)
        /// </summary>
        public static async Task<IEnumerable<Torrent>> GetTorrentsForAsync(Anime anime, string episode)
        {
            var queryDetails = anime.Name.Replace(" ", "+")
                                   .Replace("'s", "")
                                   .Replace(".", "+")
                                   .Replace(":", "")
                                   .Replace("!", "%21")
                                   .Replace("'", "%27")
                                   .Replace("-", "%2D")
                               + "+" + anime.Resolution + "+" + anime.NextEpisode();
            
            var url = new Uri("https://www.nyaa.se/?page=rss" +
                              $"&cats={EnglishTranslated}" + 
                              $"&term={queryDetails}" + 
                              $"&sort={BySeeders}");

            var document = new HtmlDocument();

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadHtml(html);
            }
            
            var result = document.DocumentNode?
                .SelectNodes("//item")?
                .Select(n => new Nyaa(n))
                .Where(n => n.Measurement.Equals("MiB") &&
                            n.Size > 5 &&
                            n.StrippedName.Contains(episode) &&
                            n.Seeders > 0);

            if (anime.NameCollection.Any(c => c.Contains(episode)))
            {
                // To account for the case that a show contains a number (e.g. 12-sai - ep 12) that is 
                // relevant to the title and or also might contain the year in case of rework/reboot 
                // (e.g. Berserk (2016)) 
                const string fullYearPattern = @"\(\d{4}\)";
                result = result?
                    .Where(nyaa => nyaa.StrippedName.Split()
                                       .Select(c => Regex.Replace(c, fullYearPattern, ""))
                                       .Count(c => c.Contains(episode))
                                   >= 2);

                /* TODO
                 * This will fail in the case that there is a show with a name that contains the 
                 * same string multiple times in the name, something like "12 Dogfighter 12 - 12"
                */

            }

            return result?.OrderByDescending(n => n.Seeders);
        }
        
    }
}