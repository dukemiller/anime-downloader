using System;

namespace anime_downloader.Enums
{
    public enum AnimeStatus
    {
        Finished, Watching, OnHold, Dropped, Considering
    }

    public static class AnimeStatusExtension
    {
        public static string ToString(this AnimeStatus status)
        {
            switch (status)
            {
                case AnimeStatus.Finished:
                    return "Finished";
                case AnimeStatus.Watching:
                    return "Watching";
                case AnimeStatus.OnHold:
                    return "On Hold";
                case AnimeStatus.Dropped:
                    return "Dropped";
                case AnimeStatus.Considering:
                    return "Considering";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }

}