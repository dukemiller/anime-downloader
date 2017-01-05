using System;
using System.ComponentModel;

namespace anime_downloader.Enums
{
    [Serializable]
    public enum Season
    {
        /// <summary>
        ///     January, February, March
        /// </summary>
        [Description("Winter")]
        Winter=1,

        /// <summary>
        ///     April, May, June
        /// </summary>
        [Description("Spring")]
        Spring,

        /// <summary>
        ///     July, August, September
        /// </summary>
        [Description("Summer")]
        Summer,

        /// <summary>
        ///     October, November, December
        /// </summary>
        [Description("Fall")]
        Fall
    }
}
