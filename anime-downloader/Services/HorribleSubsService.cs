using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Services.Abstract;
using anime_downloader.Services.Interfaces;
using HtmlAgilityPack;

namespace anime_downloader.Services
{
    public class HorribleSubsService : DownloadServiceBase
    {
        protected override ISettingsService SettingsService { get; }

        protected override IAnimeService AnimeService { get; }

        protected override WebClient Downloader { get; }

        public HorribleSubsService(ISettingsService settingsService, IAnimeService animeService)
        {
            SettingsService = settingsService;
            AnimeService = animeService;
            Downloader = new WebClient();
        }

        public override string ServiceUrl => "http://horriblesubs.info";

        public override async Task<IEnumerable<RemoteMedia>> FindAllMedia(Anime anime, int episode)
        {
            if (_nodes == null || (DateTime.Now - _lastUpdatedNodes).Minutes > 10)
                await RetrieveNodes();

            return _nodes
                .Where(item =>  // Episode is this season
                {
                    if (item.Date.HasValue) 
                       return (item.Date.Value - DateTime.Now).Days <= MaxAge;
                    return true;
                })
                .Where(item => item.StrippedName.Contains(episode.ToString()))
                .Where(item => // Name contains everything
                {
                    var title = item.StrippedWithNoEpisode.ToLower();
                    var words = anime.Name.ToLower().Split(' ').ToList();
                    return words.Count(word => title.Contains(word)) > (words.Count / 2);
                });
        }

        // 

        private DateTime _lastUpdatedNodes = DateTime.Now;

        private IEnumerable<MagnetLink> _nodes;

        /// <remarks>
        ///     A new search strategy is required, search one single page instead of a
        ///     classified 'results' page and I shouldn't research the page every single time,
        ///     so save the page then only update every 10 minutes
        /// </remarks>>
        private async Task RetrieveNodes()
        {
            var document = new HtmlDocument();
            var url = new Uri("http://horriblesubs.info/rss.php?res=720");

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadHtml(html);
            }

            _nodes = document.DocumentNode
                .SelectNodes("//item")
                .Select(ParseHorribleSubNode);

            _lastUpdatedNodes = DateTime.Now;
        }

        private static MagnetLink ParseHorribleSubNode(HtmlNode item)
        {
            var ls = item.InnerHtml.IndexOf("<link>", StringComparison.Ordinal) + 6;
            var guid = item.InnerHtml.IndexOf("<guid", StringComparison.Ordinal);
            var link = item.InnerHtml.Substring(ls, guid - ls);
            return new MagnetLink
            {
                Name = item.Element("title").InnerText,
                Remote = WebUtility.HtmlDecode(link),
                Date = DateTime.Parse(item.Element("pubdate").InnerText)
            };
        }

        private static int MaxAge => (DateTime.Now - DateTime.Parse($"{((int)CurrentSeason() - 1) * 3 + 1}/1")).Days;

        private static Season CurrentSeason()
        {
            return (Season)Math.Ceiling(Convert.ToDouble(DateTime.Now.Month) / 3);
        }
    }
}