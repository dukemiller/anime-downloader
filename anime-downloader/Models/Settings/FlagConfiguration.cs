using System;
using System.Xml.Serialization;

namespace anime_downloader.Models
{
    [Serializable]
    public class FlagConfiguration
    {
        [XmlAttribute("sort_by_reversed")]
        public bool SortByReversed { get; set; }

        [XmlAttribute("exit_on_close")]
        public bool ExitOnClose { get; set; }

        [XmlAttribute("always_show_tray")]
        public bool AlwaysShowTray { get; set; }

        [XmlAttribute("show_folders")]
        public bool IndividualShowFolders { get; set; }

        [XmlAttribute("only_whitelisted")]
        public bool OnlyWhitelisted { get; set; }
    }
}