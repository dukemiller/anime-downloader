using System;
using System.Collections.Generic;
using anime_downloader.Models;
using anime_downloader.Services.Interfaces;

namespace anime_downloader.Tests.Services
{
    public class MockAnimeService: IAnimeService
    {
        public IEnumerable<Anime> Animes { get; }

        public IEnumerable<Anime> AiringAndWatching { get; }

        public IEnumerable<Anime> NeedsUpdates { get; }

        public IEnumerable<Anime> FilteredAndSorted()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Anime> FullyWatched()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Anime> AiringAndWatchingAndNotCompleted()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Anime> HasId { get; }

        public void Add(Anime anime)
        {
            throw new NotImplementedException();
        }

        public void Remove(Anime anime)
        {
            throw new NotImplementedException();
        }

        public void Remove(string name)
        {
            throw new NotImplementedException();
        }

        public Anime ClosestAnime(string name)
        {
            throw new NotImplementedException();
        }

        public bool Synced { get; }
    }
}
