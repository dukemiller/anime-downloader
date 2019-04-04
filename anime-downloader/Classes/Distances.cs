using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Classes
{
    public readonly struct FindResultDistance
    {
        /// <summary>
        ///     Realistically the score shouldn't ever range higher than 10,000 unless it's a complete and total mismatch
        ///     and the length of the compared word is like a paragraph, so this value is just used so that if the value
        ///     compared against is zero then return that it should be not at the top of the list
        /// </summary>
        private const double ArbitraryHighValue = 10000.00;

        public FindResultDistance(string name, FindResult result)
        {
            Result = result;
            var names = name.Trim().ToLower().Split(' ');
            var distances = new List<double>();
            distances.AddRange(result.Synonyms.Split(';').Select(word => StringRelevance(names, name, word)));
            distances.Add(StringRelevance(names, name, result.Title));
            distances.Add(StringRelevance(names, name, result.English));
            Distance = distances.Min();
        }

        // 

        public FindResult Result { get; }

        public double Distance { get; }
        
        // 

        private static double StringRelevance(IEnumerable<string> names, string name, string comparison)
        {
            if (string.IsNullOrEmpty(comparison))
                return ArbitraryHighValue;

            var distance = Methods.LevenshteinDistance(comparison, name);
            var array = comparison.ToLower().Trim().Split(' ').Distinct().ToArray();
            var relevance = (double) array.Count(names.Contains) / array.Length;
            return distance * (2 - relevance);
        }
    }

    public readonly struct GroupFileDistance
    {
        private const double Bias = 2.35;

        private static readonly string[] CommonTokens = {"no", "to", "na"};

        // 

        public GroupFileDistance(IGrouping<string, AnimeFile> grouping, Anime anime)
        {
            Group = grouping;

            var name = string.IsNullOrEmpty(anime.Details.PreferredSearchTitle)
                ? anime.Name
                : anime.Details.PreferredSearchTitle;

            var namesplit = Scrub(name);
            var groupsplit = Scrub(grouping.Key);

            double distance = Methods.LevenshteinDistance(name, grouping.Key);
            var ratioToOriginal = (double) groupsplit.Count(a => namesplit.Contains(a)) / groupsplit.Length;
            var singleWordPenalty = namesplit.Length == 1 && ratioToOriginal <= 0 ? 1 : 0;
            var noMatchPenalty = namesplit.Length > 1 && ratioToOriginal <= 0 ? 8 : 0;
            var allMatchBonus = namesplit.Count(a => groupsplit.Contains(a)) == namesplit.Length ? 1 : 0;

            Distance = singleWordPenalty * 20 + distance * (Bias + noMatchPenalty - ratioToOriginal - allMatchBonus);

            // Console.WriteLine($@"{_ratioToOriginal} {_distance} {Distance} {grouping.Key}");

            /*
            Console.WriteLine($"'{anime.Name}' COMPARED TO '{grouping.Key}'");
            Console.WriteLine($"-- {_distance}, {_ratioToOriginal}, {Distance}");
            Console.WriteLine($"-- {namesplit.CommaJoined()} || {groupsplit.CommaJoined()}");
            */
        }

        //

        public double Distance { get; }

        public IGrouping<string, AnimeFile> Group { get; }

        //

        private static string[] Scrub(string str) =>
            ReplacerRegex.Replace(str.ToLower(), "")
                .Replace(":", "")
                .Replace("-", "")
                .OnlyLettersAndSpace()
                .Split(' ')
                .Distinct()
                .Except(CommonTokens)
                .ToArray();

        private static readonly Regex ReplacerRegex = new Regex(@"(:\s(\w+\s){3,}\-\s(\w+\s){2,}(\w+\s?))");
    }

    public readonly struct StringDistance<T>
    {
        public StringDistance(T item, string name, string comparison)
        {
            var nameWords = Clean(name);
            var comparisonWords = Clean(comparison);
            var distance = Methods.LevenshteinDistance(name ?? "", comparison ?? "");
            var relevance = (double) comparisonWords.Count(word => nameWords.Contains(word)) / comparisonWords.Length;
            Item = item;
            Distance = distance * (2 - relevance);
        }

        // 

        public double Distance { get; }

        public T Item { get; }

        // 

        private static string[] Clean(string input) => input?.ToLower().Trim().Split(' ').Distinct().ToArray() ?? Array.Empty<string>();
    }
}