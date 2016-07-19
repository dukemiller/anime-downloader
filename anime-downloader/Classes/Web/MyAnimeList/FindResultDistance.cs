using System.Linq;

namespace anime_downloader.Classes.Web.MyAnimeList
{
    public class FindResultDistance
    {
        /// <summary>
        ///     Realistically the score shouldn't ever range higher than 10,000 unless it's a complete and total mismatch
        ///     and the length of the compared word is like a paragraph, so this value is just used so that if the value 
        ///     compared against is zero then return that it should be not at the top of the list
        /// </summary>
        private const double ArbitraryHighValue = 10000.00;

        private string Name { get; }

        public FindResult FindResult { get; private set; }

        public double Distance { get; private set; }

        private string[] NameSplit { get; }

        public FindResultDistance(string name, FindResult findResult)
        {
            Name = name;
            FindResult = findResult;
            NameSplit = Name.Trim().ToLower().Split(' ');
            var synonymDistances = findResult.Synonyms.Split(';').Select(StringRelevance);
            Distance = new[] { StringRelevance(findResult.Title), StringRelevance(findResult.English)}.Union(synonymDistances).Min();
        }

        private double StringRelevance(string comparison)
        {
            if (comparison.IsBlank())
                return ArbitraryHighValue;

            var distance = Methods.LevenshteinDistance(comparison, Name);
            var array = comparison.ToLower().Trim().Split(' ').Distinct().ToArray();
            var relevance = (double) array.Count(a => NameSplit.Contains(a)) / array.Length;
            return distance * (2 - relevance);
        }
    }
}