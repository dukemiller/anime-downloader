using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using anime_downloader.Classes;
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

        protected override IFileService FileService { get; }

        // 

        public NyaaSiService(ISettingsRepository settingsRepository, IAnimeRepository animeRepository,
            IAnimeService animeService, IFileService fileService)
        {
            SettingsRepository = settingsRepository;
            AnimeRepository = animeRepository;
            AnimeService = animeService;
            FileService = fileService;
        }

        // 

        public override string Url => @"https://nyaa.si/";

        public override async Task<List<RemoteMedia>> FindAllMedia(Anime anime, string name, int episode)
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

            var result = document
                .SelectNodes("//item")?
                .Cast<XmlNode>()
                .Select(ToMedia(manager))
                .Where(AppropriateFilesize)
                .Where(InThisSeason(anime, episode))
                .Where(ContainsEpisodeNumber(episode));

            return result?
                       .Where(media => !media.Name.Contains("810p")) // possibly the worst workaround ive ever used
                       .OrderByDescending(media => media.Name.Contains(anime.Resolution))
                       .ThenByDescending(n => n.Health)
                       .ToList() ?? new List<RemoteMedia>();
        }

        public override async Task<List<RemoteMedia>> PotentialStartingEpisode(string name)
        {
            var document = new XmlDocument();

            var url = new Uri("https://nyaa.si/?page=rss" +
                              $"&q={TransformEpisodeSearch(name)}" +
                              "&c=1_2" +
                              "&f=0");

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadXml(html);
            }

            var manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("nyaa", "https://nyaa.si/xmlns/nyaa");

            // get every episode with a good amount of health, health is abstract but it has to atleast have more public interest than nothing
            var result = document.SelectNodes("//item")?
                .Cast<XmlNode>()
                .Select(ToMedia(manager))
                .Where(item => !item.Name.Contains("OAD"))
                .Where(item => item.Date.Map(date => (DateTime.Now - date).Days <= AnimeSeason.MaxAgeForThisSeason())
                    .ValueOr(true))
                .Where(item => item.Health > 10)
                .ToList();

            // get the first episode in any consecutive chain of episodes, e.g. [(1),2,3,(5),(16),17,(32),33]
            var episodes = result?
                .Where(item => item.Episode.HasValue)
                .Select(item => item.Episode.ValueOr(0))
                .Distinct().OrderBy(i => i)
                .Aggregate(new List<List<int>>(), (list, i) =>
                {
                    if (list.Count == 0)
                        list.Add(new List<int> {i});
                    else if (list.Last().Last() + 1 != i)
                        list.Add(new List<int> {i});
                    else
                        list.Last().Add(i);
                    return list;
                }).Select(i => i.First());

            // order by the highest health average of these items
            var highestHealth = episodes?
                .OrderByDescending(i =>
                    (int) Math.Floor(result.Where(r => r.Episode.Exists(e => e == i)).Select(r => r.Health).Average()))
                .FirstOrDefault();

            // get all episodes of that highest health
            return result?.Where(item => item.Episode.Exists(e => e == highestHealth)).ToList();
        }

        // 

        private static Func<XmlNode, RemoteMedia> ToMedia(XmlNamespaceManager manager) => item =>
        {
            var title = item.SelectSingleNode("title")?.InnerText;
            var link = item.SelectSingleNode("link")?.InnerText;

            var pubdate = item
                .SelectSingleNodeOrNone("pubDate")
                .Map(node => node.InnerText)
                .Map(DateTime.Parse);

            var seeders = item
                .SelectSingleNodeOrNone("nyaa:seeders", manager)
                .Map(node => node.InnerText)
                .Map(int.Parse);

            var downloads = item
                .SelectSingleNodeOrNone("nyaa:downloads", manager)
                .Map(node => node.InnerText)
                .Map(int.Parse);

            var size = item
                .SelectSingleNodeOrNone("nyaa:size", manager)
                .Map(node => (Regex.Replace(node.InnerText, @"[^\d\.]", ""),
                    Regex.Replace(node.InnerText, @"[\d\.]", "").Trim().ToLower()))
                .Map(pair => double.Parse(pair.Item1) *
                             (pair.Item2 == "mib" ? 1.04858 :
                                 pair.Item2 == "gib" ? 1073.74 :
                                 pair.Item2 == "kib" ? 0.001024 : 1));

            if (link != null && link.Contains("magnet:?"))
            {
                return new MagnetLink
                {
                    Name = title,
                    Remote = link,
                    Date = pubdate,
                    Downloads = downloads,
                    DirectName = HttpUtility.UrlDecode(title?.Split("&dn=")
                        .Last()
                        .Split('&')
                        .FirstOrDefault()),
                    Hash = link.Split('&')[0],
                    Seeders = seeders,
                    Trackers = link.Split("&tr=")
                        .Skip(1)
                        .Select(i => HttpUtility.UrlDecode(i.Replace("\t", "").Replace("\n", "")))
                        .ToList(),
                    Size = size
                };
            }

            return new Torrent
            {
                Name = title,
                Remote = link,
                Date = pubdate,
                Seeders = seeders,
                Downloads = downloads,
                Size = size
            };
        };

        private static Func<RemoteMedia, bool> InThisSeason(Anime anime, int episode) => media
            => media.Date
                .Map(date => (DateTime.Now - date).Days <= AnimeSeason.MaxAgeFor(anime, episode))
                .ValueOr(true);

        private static Func<RemoteMedia, bool> ContainsEpisodeNumber(int episode) => media =>
            Regex.Split(media.StrippedName, " ")
                .Any(word => Regex.IsMatch(word, $@"\b0*{episode}(?!\.5)(?:a|b|v0*[1-9]+)?\b"));

        private static Func<RemoteMedia, bool> AppropriateFilesize => media =>
            !media.Size.HasValue || media.Size.Exists(size => size > 1 && size < 3000);
    }
}