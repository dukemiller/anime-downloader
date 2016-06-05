using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace anime_downloader.Classes.Web
{
    internal class MyAnimeListNode
    {
        private XDocument _document;

        public MyAnimeListNode(Anime anime) : this(anime, anime.Episode)
        {}

        public MyAnimeListNode(Anime anime, string episode)
        {
            var status = GetStatus(anime);
            CreateDocument(episode, status, anime.Rating);
        }

        private void CreateDocument(string episode, string status, string rating)
        {
            _document = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("entry",
                    new XElement("episode", episode),
                    new XElement("status", status),
                    new XElement("score", rating),
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
                    new XElement("tags", "")
                    )
                );
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
                default:
                    status = "completed";
                    break;
            }

            return status;
        }

        public override string ToString() => _document.Declaration + "\r\n" + _document;
    }

    internal class MyAnimeListNodeDistance
    {
        public XElement Node { get; set; }

        public double Distance { get; private set; }
        
        public MyAnimeListNodeDistance(XElement node, string name, string comparisonA, string comparisonB)
        {
            Node = node;

            if (comparisonB.Equals(""))
            {
                if (comparisonA.Equals(""))
                {
                    throw new Exception();
                }

                var detailsA = new Details(comparisonA, name);
                Distance = detailsA.RelevantDistance;
            }

            else if (comparisonA.Equals(""))
            {
                if (comparisonB.Equals(""))
                {
                    throw new Exception();
                }

                var detailsB = new Details(comparisonB, name);
                Distance = detailsB.RelevantDistance;
            }

            else
            {
                var detailsA = new Details(comparisonA, name);
                var detailsB = new Details(comparisonB, name);
                var closestDetails = detailsA.Distance >= detailsB.Distance ? detailsA : detailsB;
                Distance = closestDetails.RelevantDistance;
            }
        }

        private class Details
        {
            public int Distance { get; }

            private readonly double _relevance;

            public double RelevantDistance => Distance*(2 - _relevance);

            public Details(string comparison, string name)
            {
                Distance = Methods.LevenshteinDistance(comparison, name);
                var array = comparison.Trim().Split(' ').Distinct().Select(s => s.ToLower()).ToArray();
                var nameArray = name.Split(' ').Select(s => s.ToLower());
                _relevance = (double) array.Count(a => nameArray.Contains(a)) / array.Length;
            }
        }
    }
}

/*
    episode. int
    status. int OR string. 1/watching, 2/completed, 3/onhold, 4/dropped, 6/plantowatch
    score. int
    storage_type. int (will be updated to accomodate strings soon)
    storage_value. float
    times_rewatched. int
    rewatch_value. int
    date_start. date. mmddyyyy
    date_finish. date. mmddyyyy
    priority. int
    enable_discussion. int. 1=enable, 0=disable
    enable_rewatching. int. 1=enable, 0=disable
    comments. string
    fansub_group. string
    tags. string. tags separated by commas
*/
