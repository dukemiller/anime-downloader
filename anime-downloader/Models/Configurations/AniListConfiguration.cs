using System;
using anime_downloader.Models.AniList;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class AniListConfiguration: ObservableObject
    {
        private ClientCredentials _credentials;

        public ClientCredentials Credentials
        {
            get => _credentials;
            set => Set(() => Credentials, ref _credentials, value);
        }
    }
}
