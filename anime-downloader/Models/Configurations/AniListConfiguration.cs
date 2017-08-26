using System;
using anime_downloader.Models.AniList;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class AniListConfiguration: ObservableObject
    {
        private ApiCredentials _credentials = new ApiCredentials();

        [JsonProperty("api")]
        public ApiCredentials Credentials
        {
            get => _credentials;
            set => Set(() => Credentials, ref _credentials, value);
        }
    }
}
