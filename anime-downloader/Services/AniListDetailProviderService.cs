using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AniListDetailProviderService: IDetailProviderService
    {
        private readonly IAniListApi _api;

        // 

        public AniListDetailProviderService(IAniListApi api)
        {
            _api = api;
        }

        // 

        public int GetId(Anime anime) => anime.Details.AnnId;

        public void SetId(Anime anime, int id) => anime.Details.AnnId = id;

        public async Task<(bool successful, int id)> FindId(Anime anime)
        {
            var title = string.IsNullOrEmpty(anime.Details.PreferredSearchTitle)
                ? anime.Title
                : anime.Details.PreferredSearchTitle;

            var animes = await _api.FindAnime(title);

            var nearest = animes.OrderBy(a => new[]
            {
                Methods.LevenshteinDistance(title, a.TitleEnglish),
                Methods.LevenshteinDistance(title, a.TitleRomaji)
            }.Min()).FirstOrDefault();

            if (nearest != null)
                return (true, nearest.Id);
            
            return (false, 0);
        }

        public async Task<(bool successful, bool changesMade)> FillInDetails(Anime anime)
        {
            var changesMade = false;

            if (GetId(anime) == 0)
            {
                var (successful, id) = await FindId(anime);
                if (successful)
                    SetId(anime, id);
            }

            if (GetId(anime) > 0)
            {
                var updated = await _api.GetAnime(GetId(anime), false);

                if (anime.Details.Synopsis != updated.Description)
                {
                    anime.Details.Synopsis = updated.Description;
                    changesMade = true;
                }

                if (anime.Details.Title != updated.TitleRomaji)
                {
                    anime.Details.Title = updated.TitleRomaji;
                    changesMade = true;
                }

                if (anime.Details.English != updated.TitleEnglish)
                {
                    anime.Details.English = updated.TitleEnglish;
                    changesMade = true;
                }

                if (updated.TotalEpisodes != 0 && anime.Details.TotalEpisodes != updated.TotalEpisodes)
                {
                    anime.Details.TotalEpisodes = updated.TotalEpisodes;
                    anime.Details.OverallTotal = updated.TotalEpisodes;
                    changesMade = true;
                }

                if (anime.Details.Image == null && updated.ImagePath != null)
                {
                    anime.Details.Image = updated.ImagePath;
                    changesMade = true;
                }

                // Date details

                anime.Details.Aired = new AnimeSeason
                {
                    Year = updated.StartDate.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(updated.StartDate.Month) / 3)
                };

                if (updated.EndDate is DateTime end)
                {
                    anime.Details.Ended = new AnimeSeason
                    {
                        Year = end.Year,
                        Season = (Season)Math.Ceiling(Convert.ToDouble(end.Month) / 3)
                    };
                }

                return (true, changesMade);
            }

            return (false, false);
        }
        

        // 
    }
}