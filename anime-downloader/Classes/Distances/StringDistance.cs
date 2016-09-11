using System;
using System.Linq;

namespace anime_downloader.Classes.Distances
{
    public class StringDistance<T>
    {
        private readonly double _relevance;

        private readonly double _distance;

        public double Distance => _distance * (2 - _relevance);

        public T Item { get; }

        private static Tuple<string[], string[]> Data(string name, string comparison)
        {
            var namesplit = name.ToLower().Trim().Split(' ').Distinct().ToArray();
            var groupsplit = comparison.ToLower().Trim().Split(' ').Distinct().ToArray();
            return new Tuple<string[], string[]>(namesplit, groupsplit);
        }

        public StringDistance(T item, string name, string comparison)
        {
            var data = Data(name, comparison);
            _distance = Methods.LevenshteinDistance(name, comparison);
            _relevance = (double) (data.Item2).Count(a => (data.Item1).Contains(a)) / (data.Item2).Length;
            Item = item;
        }

    }
}