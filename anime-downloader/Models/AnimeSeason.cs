using System;
using System.Xml.Serialization;
using anime_downloader.Classes;
using anime_downloader.Enums;

namespace anime_downloader.Models
{
    [Serializable]
    public class AnimeSeason
    {
        [XmlAttribute("Year")]
        public int Year { get; set; }

        [XmlAttribute("Season")]
        public Season Season { get; set; }

        [XmlIgnore]
        public int Sort => (Year * 10) + (int) Season;

        [XmlIgnore]
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
    }
}