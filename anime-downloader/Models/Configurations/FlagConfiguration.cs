using System;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class FlagConfiguration: ObservableObject
    {
        private bool _sortByReversed;
        private bool _exitOnClose;
        private bool _alwaysShowTray;
        private bool _individualShowFolders;
        private bool _onlyWhitelisted;

        [XmlAttribute("sort_by_reversed")]
        public bool SortByReversed
        {
            get { return _sortByReversed; }
            set { Set(() => SortByReversed, ref _sortByReversed, value); }
        }

        [XmlAttribute("exit_on_close")]
        public bool ExitOnClose
        {
            get { return _exitOnClose; }
            set { Set(() => ExitOnClose, ref _exitOnClose, value); }
        }

        [XmlAttribute("always_show_tray")]
        public bool AlwaysShowTray
        {
            get { return _alwaysShowTray; }
            set { Set(() => AlwaysShowTray, ref _alwaysShowTray, value); }
        }

        [XmlAttribute("show_folders")]
        public bool IndividualShowFolders
        {
            get { return _individualShowFolders; }
            set { Set(() => IndividualShowFolders, ref _individualShowFolders, value); }
        }

        [XmlAttribute("only_whitelisted")]
        public bool OnlyWhitelisted
        {
            get { return _onlyWhitelisted; }
            set { Set(() => OnlyWhitelisted, ref _onlyWhitelisted, value); }
        }
    }
}