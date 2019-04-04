using System;
using System.Linq;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Services.Interfaces;
using Optional;
using Optional.Collections;
using Optional.Unsafe;
using static anime_downloader.Classes.Methods;

namespace anime_downloader.Services
{
    public class AniListDetailProviderService : IDetailProviderService
    {
        private readonly IAniListApi _api;

        // 

        public AniListDetailProviderService(IAniListApi api) => _api = api;

        // 

        public Option<int> GetId(Anime anime) => anime.Details.AniId.Some();

        public void SetId(Anime anime, int id) => anime.Details.AniId = id;

        public async Task<Option<int>> FindId(Anime anime)
        {
            var title = string.IsNullOrEmpty(anime.Details.PreferredSearchTitle)
                ? anime.Title
                : anime.Details.PreferredSearchTitle;

            return (await _api.FindAnime(title))
                .OrderBy(airing => Methods.Enumerable
                    .Of(airing.Title.English, airing.Title.Romaji)
                    .Select(name => LevenshteinDistance(title, name))
                    .Min())
                .FirstOrNone()
                .Map(n => n.Id);
        }

        public async Task<(bool successful, bool changesMade)> FillInDetails(Anime anime)
        {
            var changesMade = false;

            if (!GetId(anime).HasValue)
                (await FindId(anime)).MatchSome(id => SetId(anime, id));

            if (GetId(anime).HasValue)
            {
                // todo: redoing this section is too annoying
                var updated = (await GetId(anime).FlatMapAsync(id => _api.GetAnime(id))).ValueOrDefault();

                if (updated is null)
                    return (false, false);

                // titles

                if (NotNullDifferent(anime.Details.Synopsis, updated.Description))
                {
                    anime.Details.Synopsis = updated.Description;
                    changesMade = true;
                }

                if (NotNullDifferent(anime.Details.Title, updated.Title.Romaji))
                {
                    anime.Details.Title = updated.Title.Romaji;
                    changesMade = true;
                }

                if (NotNullDifferent(anime.Details.English, updated.Title.English))
                {
                    anime.Details.English = updated.Title.English;
                    changesMade = true;
                }

                // episode 

                if (anime.Details.OverallTotal < anime.Details.TotalEpisodes)
                    anime.Details.OverallTotal = anime.Details.TotalEpisodes;

                if (updated.Episodes.HasValue && updated.Episodes != 0 && anime.Details.TotalEpisodes != updated.Episodes)
                {
                    anime.Details.TotalEpisodes = updated.Episodes.Value;
                    anime.Details.OverallTotal = updated.Episodes.Value;
                    changesMade = true;
                }
                
                // date 

                if (updated.StartDate.Month.HasValue && updated.StartDate.Year.HasValue)
                {
                    var aired = new AnimeSeason
                    {
                        Year = updated.StartDate.Year.Value,
                        Season = (Season) Math.Ceiling(Convert.ToDouble(updated.StartDate.Month) / 3)
                    };

                    if (anime.Details.Aired != aired)
                    {
                        anime.Details.Aired = aired;
                        changesMade = true;
                    }
                }

                if (updated.EndDate.Month.HasValue && updated.EndDate.Year.HasValue)
                {
                    var ended = new AnimeSeason
                    {
                        Year = updated.EndDate.Year.Value,
                        Season = (Season) Math.Ceiling(Convert.ToDouble(updated.EndDate.Month.Value) / 3)
                    };

                    if (anime.Details.Ended != ended)
                    {
                        anime.Details.Ended = ended;
                        changesMade = true;
                    }
                }

                // misc

                if (anime.Details.Image is null && updated.CoverImage.Large != null)
                {
                    anime.Details.Image = updated.CoverImage.Large;
                    changesMade = true;
                }

                // continuation

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
                Option<RelationNode> node;

                var current = await GetId(anime).FlatMapAsync(id => _api.GetAnime(id));

                do
                {
                    node = current.FlatMap(airing => airing.Relations
                        .Edges
                        .FirstOrNone(edge => edge.RelationType.ToLower() == "prequel")
                        .Map(edge => edge.Node)
                    );

                    if (!node.HasValue)
                        continue;

                    anime.Details.OverallTotal += node
                        .Filter(relation => relation.Format.ToLower() == "tv")
                        .Map(relation => relation.Episodes ?? 0)
                        .ValueOr(0);

                    current = await node.FlatMapAsync(r => _api.GetAnime(r.Id));

                } while (node.HasValue && anime.Episode > anime.Details.Total);

                // If after all attempts to change the episode is still greater,
                if (anime.Episode > anime.Details.Total)
                {
                    Alert($"The episode count for {anime.Name} might be an error.\n\n" +
                          $"The episode will be set from {anime.Episode} to {anime.Details.OverallTotal}, " +
                          "if this is incorrect then remove this series and attempt to re-add it.");
                    anime.Episode = anime.Details.OverallTotal;
                }

                return true;
            }

            return false;
        }

        // 

    }
}