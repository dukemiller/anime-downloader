using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace anime_downloader.Classes
{
    public static class Extensions
    {
        
        public static void AddSorted<T>(this IList<T> list, T item, IComparer<T> comparer = null)
        {
            if (comparer == null)
                comparer = Comparer<T>.Default;

            int i = 0;
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

        public static bool IsBlank(this string str)
        {
            return str == null || str.Equals("");
        }
    }
}