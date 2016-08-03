using System.Linq;
using anime_downloader.Classes.File;

namespace anime_downloader.Classes.Distances
{
    public class GroupFileDistance
    {
        private static readonly string[] CommonTokens = { "no", "na" };

        private const double Bias = 3.25;

        private readonly double _relevance;

        private readonly double _distance;

        /// <remarks>
        ///     If the whole phrase is a single word and it's nonmatching, then the penalty
        ///     will be marked 100% higher
        /// </remarks>
        private readonly int _singleWordPenalty;

        public double Distance => _distance * (Bias + _singleWordPenalty - _relevance);

        public IGrouping<string, AnimeFile> Group { get; }

        public GroupFileDistance(IGrouping<string, AnimeFile> grouping, Anime anime)
        {
            Group = grouping;
            var namesplit = anime.Name.ToLower().OnlyLettersAndSpace().Split(' ').Distinct().Except(CommonTokens).ToArray();
            var groupsplit = grouping.Key.ToLower().OnlyLettersAndSpace().Split(' ').Distinct().Except(CommonTokens).ToArray();
            _distance = Methods.LevenshteinDistance(grouping.Key, anime.Name);
            _relevance = (double) groupsplit.Count(a => namesplit.Contains(a)) / groupsplit.Length;
            _singleWordPenalty = namesplit.Length == 1 && groupsplit.Length == 1 ? 1 : 0;
            /*
            Console.WriteLine($"'{anime.Name}' COMPARED TO '{grouping.Key}'");
            Console.WriteLine($"-- {_distance}, {_relevance}, {Distance}");
            Console.WriteLine($"-- {namesplit.CommaJoined()} || {groupsplit.CommaJoined()}");
            */
        }
    }
}