using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Services.Abstract;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class NyaaPantsuService : DownloadServiceBase
    {
        private const string YearPattern = @"[^a-zA-Z](\d{4})[^a-zA-Z]";

        private const string EnglishTranslated = "3_5";

        private const string BySeeders = "5";

        protected override ISettingsService SettingsService { get; }

        protected override IAnimeService AnimeService { get; }

        protected override WebClient Downloader { get; }

        public NyaaPantsuService(ISettingsService settingsService, IAnimeService animeService)
        {
            SettingsService = settingsService;
            AnimeService = animeService;
            Downloader = new WebClient();
        }

        public override string ServiceUrl => "https://nyaa.pantsu.cat/";

        public override async Task<IEnumerable<RemoteMedia>> FindAllMedia(Anime anime, int episode)
        {
            var document = new XmlDocument();

            var url = new Uri("https://nyaa.pantsu.cat/feed" +
                              $"?c={EnglishTranslated}" +
                              "&s=" +
                              $"&sort={BySeeders}" +
                              "&order=desc" +
                              "&max=20" +
                              $"&q={NyaaTerms(anime, episode)}");

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadXml(html);
            }
            
            var result = document.SelectNodes("//item")
                ?.Cast<XmlNode>()
                .Select(ToMagnet)
                .Where(item => // Episode is this season
                {
                    if (item.Date.HasValue)
                        return (item.Date.Value - DateTime.Now).Days <= MaxAge;
                    return true;
                })
                .Where(item => Regex
                    .Split(item.StrippedName, " ")
                    .Any(s => s.Contains(episode.ToString("D2")) && !s.Contains(episode.ToString("D2") + ".5")));

            if (anime.NameCollection.Any(c => Regex.Replace(c, YearPattern, "").Contains(episode.ToString("D2"))))
                result = result?
                    .Where(nyaa => nyaa.StrippedName.Split()
                                       .Select(c => Regex.Replace(c, YearPattern, ""))
                                       .Count(c => c.Contains(episode.ToString("D2")))
                                   >= 2);

            return result?.OrderByDescending(n => n.Name.Contains(anime.Resolution)).ThenByDescending(n => n.Seeders);
        }

        private static MagnetLink ToMagnet(XmlNode item)
        {
            var magnet = new MagnetLink
            {
                DirectName = item["torrent:fileName"]?.InnerText,
                Name = item["title"]?.InnerText,
                Remote = item["torrent:magnetURI"]?.InnerText,
                Date = DateTime.Parse(item["pubDate"]?.InnerText),
                Seeders = int.Parse(item["torrent:seeds"]?.InnerText ?? "0"),
                Hash = item["torrent:magnetURI"]?.InnerText.Split('&')[0],
                Trackers = item["torrent:magnetURI"]
                    ?.InnerText.Split(new[] {"&tr="}, StringSplitOptions.None)
                    .Skip(1)
                    .ToList()
            };

            return magnet;
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