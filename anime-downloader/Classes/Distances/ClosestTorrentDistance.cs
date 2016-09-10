using System.Linq;
using anime_downloader.Classes.Web;

namespace anime_downloader.Classes.Distances
{
    public class ClosestTorrentDistance
    {
        private readonly double _relevance;

        private readonly double _distance;

        public double Distance => _distance * (2 - _relevance);

        public Torrent Torrent { get; }

        public ClosestTorrentDistance(Torrent nyaa, Anime comparison)
        {
            var animeName = nyaa.StrippedWithNoEpisode;
            var namesplit = animeName.ToLower().Trim().Split(' ').Distinct().ToArray();
            var groupsplit = comparison.Name.ToLower().Trim().Split(' ').Distinct().ToArray();
            _distance = Methods.LevenshteinDistance(animeName, comparison.Name);
            _relevance = (double) groupsplit.Count(a => namesplit.Contains(a)) / groupsplit.Length;
            Torrent = nyaa;

            /*
            Console.WriteLine($"'{animeName}' COMPARED TO '{comparison.Name}'");
            Console.WriteLine($"-- {_distance}, {_relevance}, {Distance}");
            Console.WriteLine($"-- {namesplit.CommaJoined()} || {groupsplit.CommaJoined()}");
            */
        }
    }
}