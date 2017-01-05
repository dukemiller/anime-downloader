using System;
using System.ComponentModel;
using anime_downloader.Classes;

namespace anime_downloader.Enums
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    [Serializable]
    public enum Status
    {
        [Description("Watching")] Watching,

        [Description("Considering")] Considering,

        [Description("Finished")] Finished,

        [Description("On Hold")] OnHold,

        [Description("Dropped")] Dropped
    }
}