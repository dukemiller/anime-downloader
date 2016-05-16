using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace anime_downloader.Classes.Web
{
    public class Nyaa : TorrentProvider
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

        /// <summary>
        ///     HTML Nyaa Initializer
        /// </summary>
        /// <param name="node">A raw node.</param>
        public Nyaa(HtmlNode node)
        {
            Name = WebUtility.HtmlDecode(node.Element("title").InnerText.Replace("Â", ""));
            Link = node.Element("#text").InnerText.Replace("#38;", "");
            Description = node.Element("description").InnerText;
            if (Description.Contains("CDATA"))
                Description = Description
                    .Split(new[] {"<![CDATA["}, StringSplitOptions.None)[1]
                    .Split(new[] {"]]>"}, StringSplitOptions.None)[0];
            Seeders = int.Parse(Description.Split(new[] {" seeder"}, StringSplitOptions.None)[0]);
            Measurement = ToMegabyte.First(d => Description.Contains(d.Key)).Key;
            Size = Math.Round(double.Parse(Description.Split(new[] {$" {Measurement}"}, StringSplitOptions.None)[0]
                .Split(new[] {" - "}, StringSplitOptions.None)[1])*ToMegabyte[Measurement], 2);
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

        public static async Task<IEnumerable<TorrentProvider>> GetTorrentsForAsync(Anime anime, string episode)
        {
            var url = new Uri($"https://www.nyaa.se/?page=rss&cats=1_37&term={anime.ToRss(episode)}&sort=2");
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
                            n.StrippedName().Contains(episode) &&
                            n.Seeders > 0);

            return result;
        }
    }
}