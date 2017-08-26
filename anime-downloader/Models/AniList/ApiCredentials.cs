using System;
using Newtonsoft.Json;

namespace anime_downloader.Models.AniList
{
    [Serializable]
    public class ApiCredentials
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = "";

        [JsonProperty("expires")]
        public int Expires { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonIgnore]
        public DateTime ExpiresDateTime => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Expires).ToLocalTime();
    }
}