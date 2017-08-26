using System;
using System.Xml.Serialization;
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

        [JsonProperty("sort_by_reversed")]
        public bool SortByReversed
        {
            get => _sortByReversed;
            set => Set(() => SortByReversed, ref _sortByReversed, value);
        }

        [JsonProperty("exit_on_close")]
        public bool ExitOnClose
        {
            get => _exitOnClose;
            set => Set(() => ExitOnClose, ref _exitOnClose, value);
        }

        [JsonProperty("always_show_tray")]
        public bool AlwaysShowTray
        {
            get => _alwaysShowTray;
            set => Set(() => AlwaysShowTray, ref _alwaysShowTray, value);
        }

        [JsonProperty("show_folders")]
        public bool IndividualShowFolders
        {
            get => _individualShowFolders;
            set => Set(() => IndividualShowFolders, ref _individualShowFolders, value);
        }

        [JsonProperty("only_whitelisted")]
        public bool OnlyWhitelisted
        {
            get => _onlyWhitelisted;
            set => Set(() => OnlyWhitelisted, ref _onlyWhitelisted, value);
        }
    }
}