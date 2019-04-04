using System;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class FlagConfiguration : ObservableObject
    {
        /// <summary>
        ///     An ordering flag used in the main anime list display to check if sorting is reversed.
        /// </summary>
        [JsonProperty("sort_by_reversed")]
        public bool SortByReversed { get; set; }

        /// <summary>
        ///     Determines if the program will exit when its closed or remain in the background.
        /// </summary>
        [JsonProperty("exit_on_close")]
        public bool ExitOnClose { get; set; }

        /// <summary>
        ///     Determines whether the tray icon should always display or not.
        /// </summary>
        [JsonProperty("always_show_tray")]
        public bool AlwaysShowTray { get; set; }

        /// <summary>
        ///     Determines if new folders need to be created for every show when it's downloaded.
        /// </summary>
        [JsonProperty("show_folders")]
        public bool IndividualShowFolders { get; set; }

        /// <summary>
        ///     Determines if shows should only be downloaded if their subgroup is in the whitelist.
        /// </summary>
        [JsonProperty("only_whitelisted")]
        public bool OnlyWhitelisted { get; set; }
    }
}