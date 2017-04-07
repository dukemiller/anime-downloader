using anime_downloader.Enums;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    public class MultipleAnimeDetails : ObservableObject
    {
        private bool _airing;

        private string _episode;

        private string _rating;

        private string _resolution;

        private Status _status;

        public string Resolution
        {
            get => _resolution;
            set => Set(() => Resolution, ref _resolution, value);
        }

        public bool Airing
        {
            get => _airing;
            set => Set(() => Airing, ref _airing, value);
        }

        public Status Status
        {
            get => _status;
            set => Set(() => Status, ref _status, value);
        }

        public string Episode
        {
            get => _episode;
            set => Set(() => Episode, ref _episode, value);
        }

        public string Rating
        {
            get => _rating;
            set => Set(() => Rating, ref _rating, value);
        }
    }
}