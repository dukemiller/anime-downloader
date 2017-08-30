using System;
using anime_downloader.Classes;
using anime_downloader.Enums;
using Newtonsoft.Json;

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
        public int Sort => (Year * 10) + (int) Season;

        [JsonIgnore]
        public string Title => $"{Season.Description()} {Year}";

        public AnimeSeason Next()
        {
            var season = Season == Season.Fall ? Season.Winter : Season+1;
            var year = season == Season.Winter ? Year+1 : Year;
            return new AnimeSeason
            {
                Season = season,
                Year = year
            };
        }
        
        public AnimeSeason Previous()
        {
            var season = Season - 1 <= 0 ? Season.Fall : Season - 1;
            var year = Season == Season.Winter ? Year - 1 : Year;
            return new AnimeSeason {Season = season, Year = year};
        }

        private AnimeSeason Applier(Func<AnimeSeason, AnimeSeason> func, int amount)
        {
            var that = this;
            for (var i = 0; i < amount; i++)
                that = func(that);
            return that;
        }

        public AnimeSeason Next(int amount) => Applier(animeSeason => animeSeason.Next(), amount);

        public AnimeSeason Previous(int amount) => Applier(animeSeason => animeSeason.Previous(), amount);

        public static AnimeSeason Current => new AnimeSeason
        {
            Year = DateTime.Now.Year,
            Season = (Season) Math.Ceiling(Convert.ToDouble(DateTime.Now.Month) / 3)
        };

        // 

        public static bool operator ==(AnimeSeason left, AnimeSeason right) => left?.Year == right?.Year &&
                                                                               left?.Season == right?.Season;

        public static bool operator !=(AnimeSeason left, AnimeSeason right) => !(left == right);

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