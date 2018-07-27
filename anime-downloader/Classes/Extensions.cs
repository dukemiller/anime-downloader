using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using anime_downloader.Enums;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

namespace anime_downloader.Classes
{
    public static class Extensions
    {
        public static IOrderedEnumerable<T> OrderByLevenshtein<T>(this IEnumerable<T> source, Func<T, string> keySelector, string compare)
        {
            return source.OrderBy(item => Methods.LevenshteinDistance(keySelector(item), compare));
        }

        public static IEnumerable<T> WhereLevenshteinLessThan<T>(this IEnumerable<T> source,
            Func<T, string> keySelector, string compare, int tolerance)
        {
            return source.Where(item => Methods.LevenshteinDistance(keySelector(item), compare) < tolerance);
        }
        
        public static void AddSorted<T>(this IList<T> list, T item, IComparer<T> comparer = null)
        {
            if (comparer == null)
                comparer = Comparer<T>.Default;

            var i = 0;
            while (i < list.Count && comparer.Compare(list[i], item) < 0)
                i++;

            list.Insert(i, item);
        }

        public static string OnlyLettersAndSpace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsLetter(c) || !char.IsWhiteSpace(c))
                .ToArray());
        }

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

        /// <summary>
        ///     Get a unique ioc instance of ViewModel TService.
        /// </summary>
        public static TService GetUniqueInstance<TService>(this SimpleIoc ioc) where TService: ViewModelBase
        {
            return ioc.GetInstance<TService>(Guid.NewGuid().ToString());
        }

        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        /// <summary>
        ///     Get the number of the month that this season would first air in, 
        ///     e.g. winter = 1 (jan/feb/march), spring = 4 (april/may/june) etc
        /// </summary>
        public static int ToFirstMonthAired(this Season season)
        {
            return (int) season * 3 - 2;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.Shuffle(new Random());
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            return source.ShuffleIterator(rng);
        }

        private static IEnumerable<T> ShuffleIterator<T>(
            this IEnumerable<T> source, Random rng)
        {
            var buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey> (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
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
    }
}