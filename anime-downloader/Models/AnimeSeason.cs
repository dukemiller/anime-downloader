using System;
using anime_downloader.Classes;
using anime_downloader.Enums;
using Newtonsoft.Json;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.Models
{
    [Serializable]
    public class AnimeSeason
    {
        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("season")]
        public Season Season { get; set; }

        [JsonIgnore]
        public string Title => $"{Season.Description()} {Year}";

        // 

        public AnimeSeason() { }

        public AnimeSeason(DateTime date)
        {
            Year = date.Year;
            Season = (Season) Math.Ceiling(Convert.ToDouble(date.Month) / 3);
        }

        // 

        public static AnimeSeason Next(AnimeSeason season) => season.Next();

        public static AnimeSeason Previous(AnimeSeason season) => season.Previous();

        public AnimeSeason Next()
        {
            var season = Season == Season.Fall ? Season.Winter : Season+1;
            var year = season == Season.Winter ? Year+1 : Year;
            return new AnimeSeason { Season = season, Year = year };
        }
        
        public AnimeSeason Previous()
        {
            var season = Season - 1 <= 0 ? Season.Fall : Season - 1;
            var year = Season == Season.Winter ? Year - 1 : Year;
            return new AnimeSeason {Season = season, Year = year};
        }

        public AnimeSeason Next(int amount) => Apply(Next, this, amount);

        public AnimeSeason Previous(int amount) => Apply(Previous, this, amount);

        public int Difference(AnimeSeason other)
        {
            var count = 0;
            var that = this;
            while (that > other)
            {
                that = that.Previous();
                count++;
            }

            return count;
        }

        /// <summary>
        ///     The max age (in days) a torrent can be for it to still be in season
        /// </summary>
        public static int MaxAgeFor(Anime anime, int episode)
        {
            var aired = anime.Details.Aired;

            if (aired == null)
                return MaxAgeForThisSeason();

            int days;
            do
            {
                days = (DateTime.Now - aired.StartDate()).Days;
                aired = aired.Next();
            } while (aired < Current);

            return days;
        }

        public static int MaxAgeForThisSeason() => (DateTime.Now - Current.StartDate()).Days;
        
        public DateTime StartDate()
        {
            var month = ((int) Season - 1) * 3 + 1;
            return DateTime.Parse($"{month}/1/{Year}");
        }

        public static AnimeSeason Current => new AnimeSeason
        {
            Year = DateTime.Now.Year,
            Season = (Season) Math.Ceiling(Convert.ToDouble(DateTime.Now.Month) / 3)
        };
        
        // 

        public static bool operator ==(AnimeSeason left, AnimeSeason right) => left?.Year == right?.Year &&
                                                                               left?.Season == right?.Season;

        public static bool operator !=(AnimeSeason left, AnimeSeason right) => !(left == right);

        public static bool operator <(AnimeSeason left, AnimeSeason right) => left?.Year < right?.Year || (left?.Year == right?.Year && left?.Season < right?.Season);

        public static bool operator >(AnimeSeason left, AnimeSeason right) => left?.Year > right?.Year || (left?.Year == right?.Year && left?.Season > right?.Season);

        public static bool operator >=(AnimeSeason left, AnimeSeason right) => left > right || left == right;

        public static bool operator <=(AnimeSeason left, AnimeSeason right) => left < right || left == right;

        protected bool Equals(AnimeSeason other) => Year == other.Year && Season == other.Season;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((AnimeSeason)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Year * 397) ^ (int) Season;
            }
        }
    }
}