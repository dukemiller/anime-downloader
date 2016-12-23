﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using anime_downloader.Classes;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Services
{
    public class AnimeService: IAnimeService
    {
        public ISettingsService Settings { get; set; }

        public AnimeService(ISettingsService settings)
        {
            Settings = settings;
        }

        public IEnumerable<Anime> Animes => Settings.Animes;

        public IEnumerable<Anime> FilteredAndSorted
        {
            get
            {
                var filters = Settings.FilterBy.Split('/');

                var propertyDescriptor = TypeDescriptor
                    .GetProperties(typeof(Anime))
                    .Find(Settings.SortBy, true);

                var animes = Animes;

                if (!Settings.FilterBy.IsBlank())
                    animes = animes.Where(a => filters.Any(f => f.Equals(a.Status)));

                return Settings.FlagConfig.SortByReversed
                    ? animes.OrderByDescending(x => propertyDescriptor.GetValue(x))
                    : animes.OrderBy(x => propertyDescriptor.GetValue(x));
            }
        }

        public IEnumerable<Anime> AiringAndWatching => Animes.Where(a => a.Airing && a.Status == "Watching");

        public IEnumerable<Anime> Watching => Animes.Where(a => a.Status == "Watching");

        public IEnumerable<Anime> NeedsUpdates => Animes.Where(a => a.MyAnimeList.NeedsUpdating && !a.Status.Equals("Considering"));

        public void Add(Anime anime)
        {
            Settings.Animes.Add(anime);
            Settings.Save();
        }

        public void Remove(Anime anime)
        {
            Settings.Animes.Remove(anime);
            Settings.Save();
        }
    }
}