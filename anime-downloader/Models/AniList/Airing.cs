using System;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    public class Airing
    {
        [JsonProperty("time")]
        public DateTime? Time { get; set; }

        [JsonProperty("countdown")]
        public int? Countdown { get; set; }

        [JsonProperty("next_episode")]
        public int? NextEpisode { get; set; }
    }
}