using System.Xml.Linq;
using anime_downloader.Classes;

namespace anime_downloader.Models.MyAnimeList
{
    public class UpdateShow
    {
        private readonly XDocument _document;
        
        public UpdateShow(Anime anime, string episode = null)
        {
            var details = new AnimeDetailGroup(anime, episode);

            _document = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("entry",
                    new XElement("episode", details.Episode),
                    new XElement("status", details.Status),
                    new XElement("score", details.Score),
                    new XElement("storage_type", ""),
                    new XElement("storage_value", ""),
                    new XElement("times_rewatched", ""),
                    new XElement("rewatch_value", ""),
                    new XElement("date_start", ""),
                    new XElement("date_finish", ""),
                    new XElement("priority", ""),
                    new XElement("enable_discussion", ""),
                    new XElement("enable_rewatching", ""),
                    new XElement("comments", ""),
                    new XElement("fansub_group", ""),
                    new XElement("tags", anime.Notes)
                    )
                );
        }

        public override string ToString() => _document.Declaration + "\r\n" + _document;
    }

    internal class AnimeDetailGroup
    {
        public string Status { get; set; }

        public string Episode { get; set; }

        public string Score { get; set; }

        public AnimeDetailGroup(Anime anime, string episode)
        {
            Episode = episode ?? anime.Episode;
            Status = GetStatus(anime);
            Score = anime.Rating;
        }

        private static string GetStatus(Anime anime)
        {
            // 1/watching, 2/completed, 3/onhold, 4/dropped, 6/plantowatch

            string status;

            switch (anime.Status)
            {
                case "Finished":
                    status = "completed";
                    break;

                case "Watching":
                    status = "watching";
                    break;

                case "On Hold":
                    status = "onhold";
                    break;

                case "Dropped":
                    status = "dropped";
                    break;

                case "Considering":
                    status = "plantowatch";
                    break;

                default:
                    status = "plantowatch";
                    break;
            }

            return status;
        }

    }
}
