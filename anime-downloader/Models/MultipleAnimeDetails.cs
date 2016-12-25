using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    public class MultipleAnimeDetails : ObservableObject
    {
        private bool _airing;
        private string _episode;
        private string _rating;
        private string _resolution;
        private string _status;

        public string Resolution
        {
            get { return _resolution; }
            set { Set(() => Resolution, ref _resolution, value); }
        }

        public bool Airing
        {
            get { return _airing; }
            set { Set(() => Airing, ref _airing, value); }
        }

        public string Status
        {
            get { return _status; }
            set { Set(() => Status, ref _status, value); }
        }

        public string Episode
        {
            get { return _episode; }
            set { Set(() => Episode, ref _episode, value); }
        }

        public string Rating
        {
            get { return _rating; }
            set { Set(() => Rating, ref _rating, value); }
        }
    }
}