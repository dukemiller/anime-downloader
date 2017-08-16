using System;
using System.Linq;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Classes
{
    public class FindResultDistance
    {
        /// <summary>
        ///     Realistically the score shouldn't ever range higher than 10,000 unless it's a complete and total mismatch
        ///     and the length of the compared word is like a paragraph, so this value is just used so that if the value
        ///     compared against is zero then return that it should be not at the top of the list
        /// </summary>
        private const double ArbitraryHighValue = 10000.00;

        public FindResultDistance(string name, FindResult findResult)
        {
            Name = name;
            FindResult = findResult;
            NameSplit = Name.Trim().ToLower().Split(' ');
            var synonymDistances = findResult.Synonyms.Split(';').Select(StringRelevance);
            Distance =
                new[] {StringRelevance(findResult.Title), StringRelevance(findResult.English)}.Union(synonymDistances)
                    .Min();
        }

        private string Name { get; }

        public FindResult FindResult { get; }

        public double Distance { get; }

        private string[] NameSplit { get; }

        private double StringRelevance(string comparison)
        {
            if (string.IsNullOrEmpty(comparison))
                return ArbitraryHighValue;

            var distance = Methods.LevenshteinDistance(comparison, Name);
            var array = comparison.ToLower().Trim().Split(' ').Distinct().ToArray();
            var relevance = (double) array.Count(a => NameSplit.Contains(a)) / array.Length;
            return distance * (2 - relevance);
        }
    }

    public class GroupFileDistance
    {
        private const double Bias = 2.35;

        private static readonly string[] CommonTokens = {"no", "to", "na"};

        private readonly int _allMatchBonus;

        private readonly double _distance;

        /// <remarks>
        ///     Such was the case that words with no matches would match more closely to
        ///     titles than ones with even half of the phrase matched, so if the word has
        ///     no matches then incur a harsh penalty
        /// </remarks>
        private readonly int _noMatchPenalty;

        private readonly double _ratioToOriginal;

        /// <remarks>
        ///     If the whole phrase is a single word and it's nonmatching, then the penalty
        ///     will be marked 100% higher
        /// </remarks>
        private readonly int _singleWordPenalty;

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

        public double Distance
            => _singleWordPenalty * 20 + _distance * (Bias + _noMatchPenalty - _ratioToOriginal - _allMatchBonus);

        public IGrouping<string, AnimeFile> Group { get; }

        private static string[] Scrub(string str)
        {
            return
                str.ToLower()
                    .Replace(":", "")
                    .Replace("-", "")
                    .OnlyLettersAndSpace()
                    .Split(' ')
                    .Distinct()
                    .Except(CommonTokens)
                    .ToArray();
        }
    }

    public class StringDistance<T>
    {
        private readonly double _distance;

        private readonly double _relevance;

        public StringDistance(T item, string name, string comparison)
        {
            var data = Data(name, comparison);
            _distance = Methods.LevenshteinDistance(name, comparison);
            _relevance = (double) data.Item2.Count(a => data.Item1.Contains(a)) / data.Item2.Length;
            Item = item;
        }

        public double Distance => _distance * (2 - _relevance);

        public T Item { get; }

        private static (string[], string[]) Data(string name, string comparison)
        {
            var namesplit = name.ToLower().Trim().Split(' ').Distinct().ToArray();
            var groupsplit = comparison.ToLower().Trim().Split(' ').Distinct().ToArray();
            return (namesplit, groupsplit);
        }
    }
}