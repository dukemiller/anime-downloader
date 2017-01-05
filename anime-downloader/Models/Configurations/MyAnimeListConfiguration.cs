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
        private bool _works;

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

        [XmlAttribute("working")]
        public bool Works
        {
            get { return _works; }
            set { Set(() => Works, ref _works, value); }
        }
    }
}