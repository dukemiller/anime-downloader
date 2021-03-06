﻿using System.Collections.Generic;
using System.Threading.Tasks;
using anime_downloader.Models;
using anime_downloader.Models.AniList;
using Optional;

namespace anime_downloader.Services.Interfaces
{
    public interface IAniListApi
    {
        Task<List<AiringAnime>> GetNewAnimes(AnimeSeason season);
        Task<List<AiringAnime>> GetLeftoverAnime(AnimeSeason season);
        Task<Option<AiringAnime>> GetAnime(int id);
        Task<List<AiringAnime>> FindAnime(string q);
    }
}