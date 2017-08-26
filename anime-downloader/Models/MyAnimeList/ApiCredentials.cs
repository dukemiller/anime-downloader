using System;
using Newtonsoft.Json;

namespace anime_downloader.Models.MyAnimeList
{
    [Serializable]
    public class ApiCredentials
    {
        [JsonProperty("cookies")]
        public string Cookies { get; set; } = "";

        [JsonProperty("csrf_token")]
        public string CsrfToken { get; set; } = "";

        [JsonProperty("csrf_token_last_retrieved")]
        public DateTime CsrfTokenLastRetrieved { get; set; } = DateTime.MinValue;

        [JsonIgnore]
        public bool NeedNewToken => (DateTime.Now - CsrfTokenLastRetrieved).TotalMinutes > 120;
    }
}