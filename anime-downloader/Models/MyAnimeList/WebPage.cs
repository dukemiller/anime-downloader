using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Classes;
using HtmlAgilityPack;

namespace anime_downloader.Models.MyAnimeList
{
    public static class WebPage
    {
        /// <summary>
        ///     Search for the first result of text on myanimelist and open in browser.
        /// </summary>
        public static async Task SearchAndOpenAsync(string text)
        {
            var q = HttpUtility.UrlEncode(text);
            var document = new HtmlDocument();

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(new Uri($"https://myanimelist.net/anime.php?q={q}"));
                document.LoadHtml(html);
            }

            var link = document.DocumentNode?
                .SelectSingleNode("//div[@class=\"js-categories-seasonal js-block-list list\"]/table/tr[2]/td[1]")?
                .Descendants("a")?
                .FirstOrDefault();

            if (link != null)
                Process.Start(link.Attributes["href"].Value);
            else
                Methods.Alert("No results found.");
        }
    }
}
