using System;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models
{
    [Serializable]
    public class VersionCheck: ObservableObject
    {
        private bool _needsUpdate;

        private DateTime _lastChecked;

        // 

        [JsonProperty("needs_update")]
        public bool NeedsUpdate
        {
            get => _needsUpdate;
            set => Set(() => NeedsUpdate, ref _needsUpdate, value);
        }

        [JsonProperty("last_checked")]
        public DateTime LastChecked
        {
            get => _lastChecked;
            set => Set(() => LastChecked, ref _lastChecked, value);
        }
    }
}
