using System;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.MyAnimeList;

namespace anime_downloader.Classes
{
    public static class AnimeConverter
    {
        public static Anime ToAnime(ProfileAnimeResult profileAnimeResult)
        {
            var airing = ProfileAnimeAiring(profileAnimeResult);
            var status = ProfileAnimeStatus(profileAnimeResult);

            int episode;
            int seriesEpisodes;

            var anime = new Anime
            {
                Name = profileAnimeResult.SeriesTitle,
                Episode = int.TryParse(profileAnimeResult.MyWatchedEpisodes, out episode) ? episode : 0,
                Rating = profileAnimeResult.MyScore,
                Notes = profileAnimeResult.MyTags,
                Status = status,
                Airing = airing,
                Resolution = "720",
                MyAnimeList = new MyAnimeListDetails
                {
                    Id = profileAnimeResult.SeriesAnimedbId,
                    Synonyms = profileAnimeResult.SeriesSynonyms,
                    Image = profileAnimeResult.SeriesImage,
                    Title = profileAnimeResult.SeriesTitle,
                    TotalEpisodes = int.TryParse(profileAnimeResult.SeriesEpisodes, out seriesEpisodes) ? seriesEpisodes : 0,
                    NeedsUpdating = false,
                    English = "",
                    Synopsis = ""
                }
            };

            // Date stuff
            DateTime start;
            if (DateTime.TryParse(profileAnimeResult.SeriesStart, out start))
            {
                anime.MyAnimeList.Aired = new AnimeSeason
                {
                    Year = start.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(start.Month) / 3)
                };
            }

            DateTime end;
            if (DateTime.TryParse(profileAnimeResult.SeriesEnd, out end))
            {
                anime.MyAnimeList.Ended = new AnimeSeason
                {
                    Year = end.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(end.Month) / 3)
                };
            }

            if (anime.Rating.Equals("0"))
                anime.Rating = "";

            return anime;
        }

        private static bool ProfileAnimeAiring(ProfileAnimeResult profileAnimeResult)
        {
            bool airing;
            switch (profileAnimeResult.SeriesStatus)
            {
                case "1":
                    airing = true;
                    break;

                default:
                    airing = false;
                    break;
            }

            return airing;
        }

        private static Status ProfileAnimeStatus(ProfileAnimeResult profileAnimeResult)
        {
            Status status;
            switch (profileAnimeResult.MyStatus)
            {
                case "1":
                    status = Status.Watching;
                    break;
                case "3":
                    status = Status.OnHold;
                    break;
                case "4":
                    status = Status.Dropped;
                    break;
                case "6":
                    status = Status.Considering;
                    break;
                case "2":
                default:
                    status = Status.Finished;
                    break;
            }

            return status;
        }
    }
}
