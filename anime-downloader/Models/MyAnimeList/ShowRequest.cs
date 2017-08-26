using System;
using Newtonsoft.Json;

namespace anime_downloader.Models.MyAnimeList
{
    [Serializable]
    public class ShowRequest
    {
        [JsonProperty("anime_id")]
        public int Id { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("num_watched_episodes")]
        public int Episodes { get; set; }

        [JsonProperty("csrf_token")]
        public string CsrfToken { get; set; }
    }

}
