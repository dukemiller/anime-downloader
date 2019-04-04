using System;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models
{
    [Serializable]
    public class VersionCheck: ObservableObject
    {
        [JsonProperty("needs_update")]
        public bool NeedsUpdate { get; set; }

        [JsonProperty("last_checked")]
        public DateTime LastChecked { get; set; }
    }
}
