using System;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    [Serializable]
    public class VersionCheck: ObservableObject
    {
        private bool _needsUpdate;

        private DateTime _lastChecked;

        // 

        [XmlAttribute("needs_update")]
        public bool NeedsUpdate
        {
            get => _needsUpdate;
            set => Set(() => NeedsUpdate, ref _needsUpdate, value);
        }

        [XmlAttribute("last_checked")]
        public DateTime LastChecked
        {
            get => _lastChecked;
            set => Set(() => LastChecked, ref _lastChecked, value);
        }
    }
}
