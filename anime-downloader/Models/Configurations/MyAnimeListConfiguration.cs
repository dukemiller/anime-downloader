using System;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models.Configurations
{
    [Serializable]
    public class MyAnimeListConfiguration : ObservableObject
    {
        private string _password;
        private string _username;
        private bool _loggedIn;

        [XmlAttribute("username")]
        public string Username
        {
            get { return _username; }
            set { Set(() => Username, ref _username, value); }
        }

        [XmlAttribute("password")]
        public string Password
        {
            get { return _password; }
            set { Set(() => Password, ref _password, value); }
        }

        [XmlAttribute("logged_in")]
        public bool LoggedIn
        {
            get { return _loggedIn; }
            set { Set(() => LoggedIn, ref _loggedIn, value); }
        }
    }
}