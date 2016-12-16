using System;
using System.Linq;
using anime_downloader.Classes.File;
using anime_downloader.Models;

namespace anime_downloader.Classes.Distances
{
    public class GroupFileDistance
    {
        private static readonly string[] CommonTokens = { "no", "to", "na" };

        private const double Bias = 2.35;

        private readonly double _ratioToOriginal;

        private readonly double _distance;

        /// <remarks>
        ///     If the whole phrase is a single word and it's nonmatching, then the penalty
        ///     will be marked 100% higher
        /// </remarks>
        private readonly int _singleWordPenalty;

        /// <remarks>
        ///     Such was the case that words with no matches would match more closely to
        ///     titles than ones with even half of the phrase matched, so if the word has
        ///     no matches then incur a harsh penalty
        /// </remarks>
        private readonly int _noMatchPenalty;

        private readonly int _allMatchBonus;

        public double Distance => (_singleWordPenalty * 20) + _distance * (Bias + _noMatchPenalty - _ratioToOriginal - _allMatchBonus);

        public IGrouping<string, AnimeFile> Group { get; }

        private static string[] Scrub(string str)
        {
            return str.ToLower().Replace(":", "").Replace("-", "").OnlyLettersAndSpace().Split(' ').Distinct().Except(CommonTokens).ToArray();
        }

        public GroupFileDistance(IGrouping<string, AnimeFile> grouping, Anime anime)
        {
            Group = grouping;

            var namesplit = Scrub(anime.Name);
            var groupsplit = Scrub(grouping.Key);

            _distance = Methods.LevenshteinDistance(anime.Name, grouping.Key);
            _ratioToOriginal = (double) groupsplit.Count(a => namesplit.Contains(a)) / groupsplit.Length;
            _singleWordPenalty = namesplit.Length == 1 && _ratioToOriginal <= 0 ? 1 : 0;
            _noMatchPenalty = namesplit.Length > 1 && _ratioToOriginal <= 0 ? 8 : 0;
            _allMatchBonus = namesplit.Count(a => groupsplit.Contains(a)) == namesplit.Length ? 1 : 0;

            // Console.WriteLine($@"{_ratioToOriginal} {_distance} {Distance} {grouping.Key}");
            
            /*
            Console.WriteLine($"'{anime.Name}' COMPARED TO '{grouping.Key}'");
            Console.WriteLine($"-- {_distance}, {_ratioToOriginal}, {Distance}");
            Console.WriteLine($"-- {namesplit.CommaJoined()} || {groupsplit.CommaJoined()}");
            */
            
        }
    }
}