using System;
using anime_downloader.Models.MyAnimeList;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class MyAnimeListConfiguration : ObservableObject
    {
        private string _password = "";

        private string _username = "";

        private bool _loggedIn;

        private ApiCredentials _credentials = new ApiCredentials();

        [JsonProperty("username")]
        public string Username
        {
            get => _username;
            set => Set(() => Username, ref _username, value);
        }

        [JsonProperty("password")]
        public string Password
        {
            get => _password;
            set => Set(() => Password, ref _password, value);
        }

        [JsonProperty("logged_in")]
        public bool LoggedIn
        {
            get => _loggedIn;
            set => Set(() => LoggedIn, ref _loggedIn, value);
        }

        [JsonProperty("api")]
        public ApiCredentials Credentials
        {
            get => _credentials;
            set => Set(() => Credentials, ref _credentials, value);
        }
    }
}