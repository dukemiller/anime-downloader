using System;
using anime_downloader.Enums;
using anime_downloader.Models;

namespace anime_downloader.Classes
{
    public static class Predicates
    {
        public static Func<Anime, bool> Watching => anime => 
            anime.Status == Status.Watching;

        public static Func<Anime, bool> WatchingOrFinished => anime => 
            anime.Status == Status.Watching || anime.Status == Status.Finished;

        public static Func<Anime, bool> MarkedAsAiring => anime => 
            anime.Airing;

        public static Func<Anime, bool> AiringNow => anime => 
            anime.Details.AiringNow;

        public static Func<Anime, bool> NotCompleted => anime =>
        {
            if (anime.Details.HasId && anime.Details.Total != 0)
                return anime.Episode != anime.Details.Total;
            return true;
        };

        public static Func<Anime, bool> NeedsUpdates => anime =>
            anime.Details.NeedsUpdating && anime.Status != Status.Considering;

        public static Func<Anime, bool> HasMyAnimeListId => anime => 
            anime.Details.HasId;

        public static Func<Anime, bool> OnFinalEpisode => anime =>
            (anime.Details.HasId || anime.Details.OverallTotal > 0 || anime.Details.TotalEpisodes > 0) &&
            (anime.Details.OverallTotal > 0 && anime.Episode == anime.Details.OverallTotal ||
             anime.Details.TotalEpisodes > 0 && anime.Episode == anime.Details.TotalEpisodes);

        public static Func<Anime, bool> Completed => anime => 
            anime.Details.Total != 0 && anime.Episode == anime.Details.Total;
    }
}