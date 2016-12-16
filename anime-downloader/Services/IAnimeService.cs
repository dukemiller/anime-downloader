using System.Collections.Generic;
using anime_downloader.Classes;
using anime_downloader.Models;

namespace anime_downloader.Services
{
    public interface IAnimeService
    {
        // The entire collection
        IEnumerable<Anime> Animes { get; }

        // The most accessed group types
        IEnumerable<Anime> FilteredAndSorted { get; }
        IEnumerable<Anime> AiringAndWatching { get; }
        IEnumerable<Anime> Watching { get; }
        IEnumerable<Anime> NeedsUpdates { get; }

        // Collection operations
        void Add(Anime anime);
        void Remove(Anime anime);
    }
}