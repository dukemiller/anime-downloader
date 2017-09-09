using System;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class FlagConfiguration : ObservableObject
    {
        private bool _alwaysShowTray;

        private bool _exitOnClose;

        private bool _individualShowFolders;

        private bool _onlyWhitelisted;

        private bool _sortByReversed;

        // 

        /// <summary>
        ///     An ordering flag used in the main anime list display to check if sorting is reversed.
        /// </summary>
        [JsonProperty("sort_by_reversed")]
        public bool SortByReversed
        {
            get => _sortByReversed;
            set => Set(() => SortByReversed, ref _sortByReversed, value);
        }

        /// <summary>
        ///     Determines if the program will exit when its closed or remain in the background.
        /// </summary>
        [JsonProperty("exit_on_close")]
        public bool ExitOnClose
        {
            get => _exitOnClose;
            set => Set(() => ExitOnClose, ref _exitOnClose, value);
        }

        /// <summary>
        ///     Determines whether the tray icon should always display or not.
        /// </summary>
        [JsonProperty("always_show_tray")]
        public bool AlwaysShowTray
        {
            get => _alwaysShowTray;
            set => Set(() => AlwaysShowTray, ref _alwaysShowTray, value);
        }

        /// <summary>
        ///     Determines if new folders need to be created for every show when it's downloaded.
        /// </summary>
        [JsonProperty("show_folders")]
        public bool IndividualShowFolders
        {
            get => _individualShowFolders;
            set => Set(() => IndividualShowFolders, ref _individualShowFolders, value);
        }

        /// <summary>
        ///     Determines if shows should only be downloaded if their subgroup is in the whitelist.
        /// </summary>
        [JsonProperty("only_whitelisted")]
        public bool OnlyWhitelisted
        {
            get => _onlyWhitelisted;
            set => Set(() => OnlyWhitelisted, ref _onlyWhitelisted, value);
        }
    }
}