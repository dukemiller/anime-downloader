using System;
using System.Collections.Generic;
using anime_downloader.Models.MyAnimeList;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class MyAnimeListConfiguration : ObservableObject
    {
        [JsonProperty("username")]
        public string Username { get; set; } = "";

        [JsonProperty("password")]
        public string Password { get; set; } = "";

        [JsonProperty("logged_in")]
        public bool LoggedIn { get; set; }

        [JsonProperty("ids")]
        public List<int> Ids { get; set; } = new List<int>();

        [JsonProperty("last_checked_ids")]
        public DateTime LastCheckedIds { get; set; } = DateTime.MinValue;

        [JsonProperty("api")]
        public ApiCredentials Credentials { get; set; } = new ApiCredentials();
    }
}