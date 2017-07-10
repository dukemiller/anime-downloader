using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using anime_downloader.Models;
using anime_downloader.Models.Abstract;
using anime_downloader.Services.Abstract;
using anime_downloader.Services.Interfaces;

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
        
        public override async Task<IEnumerable<RemoteMedia>> FindAllMedia(Anime anime, string name, int episode)
        {
            if (_nodes == null || (DateTime.Now - _lastUpdatedNodes).Minutes > 10)
                await RetrieveNodes();

            return _nodes
                .Where(item => // Episode is this season
                {
                    if (item.Date.HasValue)
                        return (item.Date.Value - DateTime.Now).Days <= MaxAge;
                    return true;
                })
                .Where(item => item.StrippedName.Contains(episode.ToString()) 
                                && !item.StrippedName.Contains(episode.ToString() + ".5"))
                .Where(item => // Name contains everything
                {
                    var title = item.StrippedWithNoEpisode.ToLower();
                    var words = name.ToLower().Split(' ').ToList();
                    return words.Count(word => title.Contains(word)) > (words.Count / 2);
                })
                .OrderByDescending(n => n.Name.Contains(anime.Resolution));
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
            var document = new XmlDocument();
            var url = new Uri("http://horriblesubs.info/rss.php?res=720");

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(url);
                document.LoadXml(html);
            }

            _nodes = document.SelectNodes("//item")?
                .Cast<XmlNode>()
                .Select(ParseHorribleSubNode);

            _lastUpdatedNodes = DateTime.Now;
        }

        private static MagnetLink ParseHorribleSubNode(XmlNode item)
        {
            (var title, var link, var pubdate) =
                (item.SelectSingleNode("title")?.InnerText, 
                item.SelectSingleNode("link")?.InnerText, 
                DateTime.Parse(item.SelectSingleNode("pubDate")?.InnerText));

            var magnet = new MagnetLink
            {
                Name = title,
                Remote = link,
                Date = pubdate,
                Hash = link.Split('&')[0],
                Trackers = link.Split(new[] { "&tr=" }, StringSplitOptions.None).Skip(1).ToList()
            };

            return magnet;
        }
        
    }
}