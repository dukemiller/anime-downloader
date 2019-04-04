using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using anime_downloader.Models;

namespace anime_downloader.Classes.Xaml
{
    public class RatingSort : IComparer, IComparer<Anime>
    {
        private readonly ListSortDirection _direction;

        // 

        public RatingSort(ListSortDirection direction) => _direction = direction;

        // 

        public int Compare(object x, object y) => Compare(x as Anime, y as Anime);

        public int Compare(Anime x, Anime y)
        {
            var a = ToValue(x);
            var b = ToValue(y);
            return _direction == ListSortDirection.Ascending
                ? a.CompareTo(b)
                : b.CompareTo(a);
        }

        // 

        private int ToValue(Anime anime) =>
            string.IsNullOrEmpty(anime?.Rating)
                ? 13 * (_direction == ListSortDirection.Ascending ? 1 : -1) - 2
                : int.Parse(anime?.Rating);
    }

    public class AiringSort : IComparer, IComparer<Anime>
    {
        private readonly ListSortDirection _direction;

        // 

        public AiringSort(ListSortDirection direction) => _direction = direction;

        // 

        public int Compare(object x, object y) => Compare(x as Anime, y as Anime);

        public int Compare(Anime x, Anime y)
        {
            var a = ToValue(x);
            var b = ToValue(y);
            return _direction == ListSortDirection.Ascending
                ? a.CompareTo(b)
                : b.CompareTo(a);
        }

        // 

        private int ToValue(Anime anime) =>
            anime?.Details?.Aired != null
                ? ToValue(anime.Details.Aired)
                : (DateTime.Now.Year + 3) * (_direction == ListSortDirection.Ascending ? 1 : -1) - 2;

        private static int ToValue(AnimeSeason season) => 
            season.Year * 10 + (int) season.Season;
    }
}