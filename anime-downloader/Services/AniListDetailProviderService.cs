using System;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
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

        public int GetId(Anime anime) => anime.Details.AniId;

        public void SetId(Anime anime, int id) => anime.Details.AniId = id;

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

                if (anime.Details.OverallTotal < anime.Details.TotalEpisodes)
                    anime.Details.OverallTotal = anime.Details.TotalEpisodes;

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

                var aired = new AnimeSeason
                {
                    Year = updated.StartDate.Year,
                    Season = (Season)Math.Ceiling(Convert.ToDouble(updated.StartDate.Month) / 3)
                };

                if (anime.Details.Aired != aired)
                {
                    anime.Details.Aired = aired;
                    changesMade = true;
                }

                if (updated.EndDate is DateTime end)
                {
                    var ended = new AnimeSeason
                    {
                        Year = end.Year,
                        Season = (Season)Math.Ceiling(Convert.ToDouble(end.Month) / 3)
                    };

                    if (anime.Details.Ended != ended)
                    {
                        anime.Details.Ended = ended;
                        changesMade = true;
                    }
                }

                var continuationChanged = await CheckSeriesContinuation(anime);

                if (continuationChanged)
                    changesMade = true;

                return (true, changesMade);
            }

            return (false, false);
        }

        public async Task<bool> CheckSeriesContinuation(Anime anime)
        {
            // If there is a total episode and the current episode is greater than the total
            if (anime.Details.TotalEpisodes != 0 && anime.Episode > anime.Details.Total)
            {
                // Increase the overall total by checking for previous series and summing them
                anime.Details.OverallTotal = anime.Details.TotalEpisodes;

                // Loop through every prequel series, summing up the total episode counts
                Relation relation;
                var current = await _api.GetAnime(GetId(anime), fullProfile: true);

                do
                {
                    relation = current.Relations.FirstOrDefault(r => r.RelationType == "prequel");
                    if (relation == null)
                        continue;
                    if (relation.Type == "TV")
                        anime.Details.OverallTotal += relation.TotalEpisodes ?? 0;
                    current = await _api.GetAnime(relation.Id, fullProfile: true);
                } while (relation != null && anime.Episode > anime.Details.Total);

                // If after all attempts to change the episode is still greater,
                if (anime.Episode > anime.Details.Total)
                {
                    Methods.Alert($"The episode count for {anime.Name} might be an error.\n\n" +
                                  $"The episode will be set from {anime.Episode} to {anime.Details.OverallTotal}, " +
                                  $"if this is incorrect then remove this series and attempt to re-add it.");
                    anime.Episode = anime.Details.OverallTotal;
                }

                return true;

            }
            return false;
        }

        // 
    }
}