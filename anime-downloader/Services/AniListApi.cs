using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using anime_downloader.Services.Interfaces;
using Newtonsoft.Json;

namespace anime_downloader.Services
{
    // http://anilist-api.readthedocs.io/en/latest/anime.html#browse
    public class AniListApi : IAniListApi
    {
        private readonly HttpClient _client;

        private const string Url = "https://graphql.anilist.co";

        // 

        public AniListApi()
        {
            _client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });
        }

        // 

        public async Task<List<AiringAnime>> GetNewAnimes(AnimeSeason season)
        {
            var variables = new Dictionary<string, object>
            {
                {"page", 1},
                {"season", season.Season.Description().ToUpper()},
                {"seasonYear", season.Year}
            };

            return await Fetch(AnilistQueries.FetchSeason, variables);
        }

        public async Task<List<AiringAnime>> GetLeftoverAnime(AnimeSeason season)
        {
            var variables = new Dictionary<string, object>
            {
                {"page", 1},
                {"season", season.Previous().Season.Description().ToUpper()},
                {"seasonYear", season.Previous().Year},
            };

            return (await Fetch(AnilistQueries.FetchSeasonOnlyAiring, variables))
                .Where(anime => anime.Episodes.HasValue && Methods.InRange(anime.Episodes.Value, 10, 24))
                .Where(anime => anime.Episodes.Value > 12)
                .ToList();
        }

        public async Task<AiringAnime> GetAnime(int id)
        {
            var variables = new Dictionary<string, object>
            {
                {"page", 1},
                {"id", id}
            };

            return (await Fetch(AnilistQueries.Get, variables)).FirstOrDefault();
        }

        public async Task<List<AiringAnime>> FindAnime(string q)
        {
            var variables = new Dictionary<string, object>
            {
                {"page", 1},
                {"search", q}
            };

            return await Fetch(AnilistQueries.Find, variables);
        }

        // 

        private async Task<List<AiringAnime>> Fetch(string query, Dictionary<string, object> variables)
        {
            try
            {
                var pairs = new Dictionary<string, string>
                {
                    {"query", query},
                    {"variables", JsonConvert.SerializeObject(variables)}
                };
                var payload = new FormUrlEncodedContent(pairs);
                var request = await _client.PostAsync(Url, payload);
                var response = await request.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<AniListResponse>(response);
                return data.Data.Page.Media.ToList();
            }

            catch (Exception e)
            {
                return new List<AiringAnime>();
            }
        }
    }

    internal static class AnilistQueries
    {
        public const string FetchSeason = @"
            query ($page: Int, $season: MediaSeason, $seasonYear: Int) {
              Page(page: $page, perPage: 50) {
                
                pageInfo {
                  currentPage
                  hasNextPage
                }
                
                media(season: $season, seasonYear: $seasonYear, sort: POPULARITY_DESC, format_in: [TV, TV_SHORT]) {
                  " + Media + @"
                }
              }
            }
            ";

        public const string Get = @"
            query ($page: Int, $id: Int) {
              Page(page: $page, perPage: 50) {
                
                pageInfo {
                  currentPage
                  hasNextPage
                }
                
                media(format_in: [TV, TV_SHORT], id: $id) {
                " + Media + @"
                }
              }
            }";

        public const string Find = @"
            query ($page: Int, $search: String) {
              Page(page: $page, perPage: 50) {
                
                pageInfo {
                  currentPage
                  hasNextPage
                }
                
                media(format_in: [TV, TV_SHORT], search: $search) {
                " + Media + @"
                }
              }
            }";

        public const string FetchSeasonOnlyAiring = @"
            query ($page: Int, $season: MediaSeason, $seasonYear: Int) {
              Page(page: $page, perPage: 50) {
                
                pageInfo {
                  currentPage
                  hasNextPage
                }
                
                media(season: $season, seasonYear: $seasonYear, sort: POPULARITY_DESC, format_in: [TV, TV_SHORT], status:RELEASING) {
                " + Media + @"
                }
              }
            }";


        private const string Media = @"
            id
            idMal

            title {
              romaji
              english
            }

            genres
                
            startDate {
              year
              month
              day
            }

            endDate {
              year
              month
              day
            }

            format

            relations {
              edges {
                relationType
                node {
                  id
                  format
                  episodes
                }
              }
            }

            episodes

            coverImage {
              large
            }

            source

            description(asHtml: false)

            studios {
              edges {
                isMain
                node {
                  name
                }
              }
            }";
    }
}