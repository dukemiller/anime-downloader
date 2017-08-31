using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Abstract;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class NyaaSiService : DownloadServiceBase
    {
        protected override ISettingsRepository SettingsRepository { get; }

        protected override IAnimeRepository AnimeRepository { get; }

        protected override IAnimeService AnimeService { get; }

        protected override WebClient Downloader { get; }

        public override string ServiceUrl => @"https://nyaa.si/";

        public NyaaSiService(ISettingsRepository settingsRepository, IAnimeRepository animeRepository, IAnimeService animeService)
        {
            SettingsRepository = settingsRepository;
            AnimeRepository = animeRepository;
            AnimeService = animeService;
            Downloader = new WebClient();
        }
        
        public override async Task<IEnumerable<RemoteMedia>> FindAllMedia(Anime anime, string name, int episode)
        {
            var document = new XmlDocument();
            
            var url = new Uri("https://nyaa.si/?page=rss" +
                              $"&q={TransformEpisodeSearch(name, episode)}" +
                              "&c=1_2" +
                              "&f=0");

            using (var client = new WebClient())
            {                                                                                       
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadXml(html);
            }

            var manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("nyaa", "https://nyaa.si/xmlns/nyaa");

            var result = document.SelectNodes("//item")?
                .Cast<XmlNode>()
                .Select(node => ToMedia(node, manager))
                .Where(item => // Episode is this season
                {
                    if (item.Date.HasValue)
                        return (item.Date.Value - DateTime.Now).Days <= MaxAge;
                    return true;
                })
                .Where(item => Regex.Split(item.StrippedName, " ")
                    .Any(s => s.Contains(episode.ToString("D2")) && !s.Contains(episode.ToString("D2") + ".5")));

            return result?.OrderByDescending(n => n.Name.Contains(anime.Resolution)).ThenByDescending(n => n.Health);
        }
        
        private static RemoteMedia ToMedia(XmlNode item, XmlNamespaceManager manager)
        {
            var title = item.SelectSingleNode("title")?.InnerText;
            var link = item.SelectSingleNode("link")?.InnerText;
            var pubdate = DateTime.Parse(item.SelectSingleNode("pubDate")?.InnerText);
            var seeders = int.Parse(item.SelectSingleNode("nyaa:seeders", manager)?.InnerText ?? "0");
            var downloads = int.Parse(item.SelectSingleNode("nyaa:downloads", manager)?.InnerText ?? "0");

            if (link != null && link.Contains("magnet:?"))
            {
                return new MagnetLink
                {
                    Name = title,
                    Remote = link,
                    Date = pubdate,
                    Downloads = downloads,
                    DirectName = HttpUtility.UrlDecode(title?.Split(new[] {"&dn="}, StringSplitOptions.None)
                        .Last()
                        .Split('&')
                        .FirstOrDefault()),
                    Hash = link.Split('&')[0],
                    Seeders = seeders,
                    Trackers = link.Split(new[] {"&tr="}, StringSplitOptions.None)
                        .Skip(1)
                        .Select(i => HttpUtility.UrlDecode(i.Replace("\t", "").Replace("\n", "")))
                        .ToList()
                };
            }

            else
                return new Torrent
                {
                    Name = title,
                    Remote = link,
                    Date = pubdate,
                    Seeders = seeders,
                    Downloads = downloads,
                };
        }
    }
}