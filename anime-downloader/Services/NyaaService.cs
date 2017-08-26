using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Repositories.Interface;
using anime_downloader.Services.Abstract;
using anime_downloader.Services.Interfaces;
using HtmlAgilityPack;

namespace anime_downloader.Services
{
    public class NyaaService : DownloadServiceBase
    {
        private const string YearPattern = @"[^a-zA-Z](\d{4})[^a-zA-Z]";

        private const string EnglishTranslated = "1_37";

        private const string BySeeders = "2";

        // 

        public NyaaService(ISettingsRepository settingsRepository, IAnimeRepository animeRepository, IAnimeService animeService)
        {
            SettingsRepository = settingsRepository;
            AnimeRepository = animeRepository;
            AnimeService = animeService;
            Downloader = new WebClient();
        }

        // 

        public override string ServiceUrl => "https://www.nyaa.se/";

        protected override ISettingsRepository SettingsRepository { get; }

        protected override IAnimeRepository AnimeRepository { get; }

        protected override IAnimeService AnimeService { get; }

        protected override WebClient Downloader { get; }

        public override async Task<IEnumerable<RemoteMedia>> FindAllMedia(Anime anime, string name, int episode)
        {
            var document = new HtmlDocument();

            var url = new Uri("https://www.nyaa.se/?page=rss" +
                              $"&cats={EnglishTranslated}" +
                              $"&term={NyaaTerms(name, episode)}" +
                              $"&sort={BySeeders}" +
                              $"&minage=0&maxage={MaxAge}");

            Console.WriteLine(url);

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadHtml(html);
            }

            return ParseResults(anime, episode, document);
        }

        // 

        private static string NyaaTerms(string name, int episode)
        {
            var term = name
                .Replace(" ", "+")
                .Replace("'s", "")
                .Replace(".", "+")
                .Replace(":", " ")
                .Replace("!", "%21")
                .Replace("'", "%27")
                .Replace("-", "%2D");
            return $"{term}+{episode:D2}";
        }

        private static IEnumerable<Torrent> ParseResults(Anime anime, int episode, HtmlDocument document)
        {
            var result = document.DocumentNode?
                .SelectNodes("//item")
                ?
                .Select(node => node.ToTorrent())
                .Where(n => n.Measurement.Equals("MiB")
                            && n.Size > 5
                            && Regex.Split(n.StrippedName, " ").Any(s => s.Contains(episode.ToString("D2")))
                            && !n.Name.ToLower().Contains("(movie)")
                            && !n.Name.ToLower()
                                .Replace("-", "")
                                .Replace("_", "")
                                .Replace(" ", "")
                                .Contains($"part{episode:D2}")
                );

            if (anime.NameCollection.Any(c => Regex.Replace(c, YearPattern, "").Contains(episode.ToString("D2"))))
                result = result?
                    .Where(nyaa => nyaa.StrippedName.Split()
                                       .Select(c => Regex.Replace(c, YearPattern, ""))
                                       .Count(c => c.Contains(episode.ToString("D2")))
                                   >= 2);

            return result?.OrderByDescending(n => n.Name.Contains(anime.Resolution)).ThenByDescending(n => n.Seeders);
        }
    }

    internal static class NyaaExtension
    {
        private static readonly Dictionary<string, double> ToMegabyte = new Dictionary<string, double>
        {
            {"MiB", 1.04858},
            {"GiB", 1073.74},
            {"KiB", 0.001024}
        };

        public static Torrent ToTorrent(this HtmlNode node)
        {
            var torrent = new Torrent
            {
                Name = WebUtility.HtmlDecode(node.Element("title").InnerText.Replace("Â", "")),
                Remote = node.Element("#text").InnerText.Replace("#38;", "")
            };

            var description = node.Element("description").InnerText;
            if (description.Contains("CDATA"))
                description = description
                    .Split(new[] {"<![CDATA["}, StringSplitOptions.None)[1]
                    .Split(new[] {"]]>"}, StringSplitOptions.None)[0];
            torrent.Description = description;
            torrent.Seeders = int.Parse(description.Split(new[] {" seeder"}, StringSplitOptions.None)[0]);
            torrent.Measurement = ToMegabyte.First(d => description.Contains(d.Key)).Key;
            torrent.Size =
                Math.Round(double.Parse(description.Split(new[] {$" {torrent.Measurement}"}, StringSplitOptions.None)[0]
                               .Split(new[] {" - "}, StringSplitOptions.None)[1]) * ToMegabyte[torrent.Measurement], 2);
            return torrent;
        }
    }
}