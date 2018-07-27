﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Abstract;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class NyaaPantsuService : DownloadServiceBase
    {
        private const string YearPattern = @"[^a-zA-Z](\d{4})[^a-zA-Z]";

        private const string EnglishTranslated = "3_5";

        private const string BySeeders = "5";

        protected override ISettingsRepository SettingsRepository { get; }

        protected override IAnimeRepository AnimeRepository { get; }

        protected override IAnimeService AnimeService { get; }

        protected override WebClient Downloader { get; }

        public NyaaPantsuService(ISettingsRepository settingsRepository, IAnimeRepository animeRepository, IAnimeService animeService)
        {
            SettingsRepository = settingsRepository;
            AnimeRepository = animeRepository;
            AnimeService = animeService;
            Downloader = new WebClient();
        }

        public override string ServiceUrl => "https://nyaa.pantsu.cat/";

        public override async Task<List<RemoteMedia>> FindAllMedia(Anime anime, string name, int episode)
        {
            var document = new XmlDocument();

            var url = new Uri("https://nyaa.pantsu.cat/feed" +
                              $"?c={EnglishTranslated}" +
                              "&s=" +
                              $"&sort={BySeeders}" +
                              "&order=desc" +
                              "&max=20" +
                              $"&q={TransformEpisodeSearch(name, episode)}");

            // https://nyaa.pantsu.cat/feed
            // ?c=3_5&limit=50&order=false&q=new+game%21%21&s=0&sort=5&userID=0

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadXml(html);
            }

            Console.WriteLine(document);
            
            var result = document.SelectNodes("//item")
                ?.Cast<XmlNode>()
                .Select(ToTorrent)
                .Where(item => // Episode is this season
                {
                    if (item.Date.HasValue)
                        return (item.Date.Value - DateTime.Now).Days <= AnimeSeason.MaxAgeFor(anime, episode);
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

            return result?.OrderByDescending(n => n.Name.Contains(anime.Resolution)).Cast<RemoteMedia>().ToList();
        }

        private static Torrent ToTorrent(XmlNode item)
        {
            var torrent = new Torrent
            {
                Name = item["title"]?.InnerText,
                Remote = item["link"]?.InnerText,
                Date = DateTime.ParseExact(item["pubDate"]?.InnerText, "dd MMM yy HH:mm UTC", CultureInfo.CurrentCulture),
            };

            return torrent;
        }

        /// <summary>
        ///     TODO::
        /// </summary>
        public override Task<List<RemoteMedia>> PotentialStartingEpisode(string name) => null;
    }
}