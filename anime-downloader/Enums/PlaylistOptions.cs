using System;

namespace anime_downloader.Enums
{
    [Flags]
    public enum PlaylistOptions
    {
        None = 0,
        SeparateShowOrder = 1,
        Reverse = 2,
        AdditionalEpisodesFirst = 4
    }
}