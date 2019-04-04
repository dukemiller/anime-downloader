using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using anime_downloader.Enums;
using HtmlAgilityPack;
using Optional;

namespace anime_downloader.Classes
{
    public static class Extensions
    {
        // specifically string types

        public static string[] Split(this string str, params string[] delimiters) =>
            str.Split(delimiters, StringSplitOptions.None);

        public static string OnlyLettersAndSpace(this string input) =>
            new string(input.ToCharArray()
                .Where(c => !char.IsLetter(c) || !char.IsWhiteSpace(c))
                .ToArray());

        // reference types

        // http://www.pavey.me/2015/04/aspnet-c-extension-method-to-get-enum.html
        public static string Description(this Enum value)
        {
            // variables  
            var enumType = value.GetType();
            var field = enumType.GetField(value.ToString());
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);

            // return  
            return attributes.Length == 0 ? value.ToString() : ((DescriptionAttribute) attributes[0]).Description;
        }

        public static HtmlDocument LoadPage(this HtmlDocument document, string html)
        {
            document?.LoadHtml(html);
            return document;
        }

        public static Option<XmlNode> SelectSingleNodeOrNone(this XmlNode node, string xpath) 
            => node.SelectSingleNode(xpath).SomeNotNull();

        public static Option<XmlNode> SelectSingleNodeOrNone(this XmlNode node, string xpath, XmlNamespaceManager msngr) 
            => node.SelectSingleNode(xpath, msngr).SomeNotNull();

        // struct types

        /// <summary>
        ///     Get the number of the month that this season would first air in, 
        ///     e.g. winter = 1 (jan/feb/march), spring = 4 (april/may/june) etc
        /// </summary>
        public static int ToFirstMonthAired(this Season season)
        {
            return (int) season * 3 - 2;
        }

        // collections

        public static void AddSorted<T>(this IList<T> list, T item, IComparer<T> comparer = null)
        {
            if (comparer is null)
                comparer = Comparer<T>.Default;

            var i = 0;
            while (i < list.Count && comparer.Compare(list[i], item) < 0)
                i++;

            list.Insert(i, item);
        }

        /// <summary>
        ///     On an enumerable of [bool], this is just a shortcut to check that all values are true
        /// </summary>
        public static bool All(this IEnumerable<bool> source) => source.All(value => value);

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.Shuffle(new Random());
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (rng is null) throw new ArgumentNullException(nameof(rng));
            return source.ShuffleIterator(rng);
        }

        private static IEnumerable<T> ShuffleIterator<T>(this IEnumerable<T> source, Random rng)
        {
            var buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static IOrderedEnumerable<T> OrderByLevenshtein<T>(this IEnumerable<T> source, Func<T, string> keySelector, string compare)
        {
            return source.OrderBy(item => Methods.LevenshteinDistance(keySelector(item), compare));
        }

        public static IEnumerable<T> WhereLevenshteinLessThan<T>(this IEnumerable<T> source, Func<T, string> keySelector, string compare, int tolerance)
        {
            return source.Where(item => Methods.LevenshteinDistance(keySelector(item), compare) < tolerance);
        }

        /// <summary>
        ///     Pass the item of type T as an arugment to the constructor of type T2
        /// </summary>
        public static IEnumerable<T2> Construct<T, T2>(this IEnumerable<T> source) => 
            source.Select(item => (T2)Activator.CreateInstance(typeof(T2), item));
    }
}