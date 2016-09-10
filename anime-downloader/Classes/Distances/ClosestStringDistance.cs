using System.Linq;

namespace anime_downloader.Classes.Distances
{
    public class ClosestStringDistance
    {
        private readonly double _relevance;

        private readonly double _distance;

        public double Distance => _distance * (2 - _relevance);

        public Anime Anime { get; }

        public ClosestStringDistance(string animeName, Anime comparison)
        {
            var namesplit = animeName.ToLower().Trim().Split(' ').Distinct().ToArray();
            var groupsplit = comparison.Name.ToLower().Trim().Split(' ').Distinct().ToArray();
            _distance = Methods.LevenshteinDistance(animeName, comparison.Name);
            _relevance = (double) groupsplit.Count(a => namesplit.Contains(a)) / groupsplit.Length;
            Anime = comparison;
        }
    }
}