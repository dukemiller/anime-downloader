using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Services.Abstract;
using anime_downloader.Services.Interfaces;
using HtmlAgilityPack;

namespace anime_downloader.Services
{
    public class NyaaSiService : DownloadServiceBase
    {
        protected override ISettingsService SettingsService { get; }

        protected override IAnimeService AnimeService { get; }

        protected override WebClient Downloader { get; }

        public override string ServiceUrl => @"https://nyaa.si/";

        public NyaaSiService(ISettingsService settingsService, IAnimeService animeService)
        {
            SettingsService = settingsService;
            AnimeService = animeService;
            Downloader = new WebClient();
        }
        
        public override async Task<IEnumerable<RemoteMedia>> FindAllMedia(Anime anime, int episode)
        {
            var document = new HtmlDocument();
            
            var url = new Uri("https://nyaa.si/?page=rss" +
                              $"&q={NyaaTerms(anime, episode)}" +
                              "&c=0_0" +
                              "&f=0");

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadHtml(html);
            }

            var result = document.DocumentNode
                .SelectNodes("//item")
                ?.Select(ToMedia)
                .Where(item => // Episode is this season
                {
                    if (item.Date.HasValue)
                        return (item.Date.Value - DateTime.Now).Days <= MaxAge;
                    return true;
                })
                .Where(item => Regex.Split(item.StrippedName, " ")
                                    .Any(s => s.Contains(episode.ToString("D2")) && !s.Contains(episode.ToString("D2") + ".5")))
                .OrderByDescending(n => n.Name.Contains(anime.Resolution));

            return result;
        }
        
        private static RemoteMedia ToMedia(HtmlNode item)
        {
            var name = item.Element("title").InnerText;
            var date = DateTime.Parse(item.Element("pubdate").InnerText);
            var ls = item.InnerHtml.IndexOf("<link>", StringComparison.Ordinal) + 6;
            var guid = item.InnerHtml.IndexOf("<guid", StringComparison.Ordinal);
            var remote = WebUtility.HtmlDecode(item.InnerHtml.Substring(ls, guid - ls));

            if (remote.Contains("magnet:?"))
            {
                return new MagnetLink
                {
                    Name = name,
                    Remote = remote,
                    Date = date,
                    DirectName = HttpUtility.UrlDecode(remote.Split(new[] {"&dn="}, StringSplitOptions.None)
                        .Last()
                        .Split('&')
                        .FirstOrDefault()),
                    Hash = remote.Split('&')[0],
                    Trackers = remote.Split(new[] {"&tr="}, StringSplitOptions.None)
                        .Skip(1)
                        .Select(i => HttpUtility.UrlDecode(i.Replace("\t", "").Replace("\n", "")))
                        .ToList()
                };
            }

            else
                return new Torrent {Name = name, Remote = remote, Date = date};
        }
        
        private static string NyaaTerms(Anime anime, int episode)
        {
            var name = anime.Name
                .Replace(" ", "+")
                .Replace("'s", "")
                .Replace(".", "+")
                .Replace(":", " ")
                .Replace("!", "%21")
                .Replace("'", "%27")
                .Replace("-", "%2D");
            return $"{name}+{episode:D2}";
        }
    }
}