using System.ComponentModel;
using System.Runtime.CompilerServices;
using anime_downloader.Annotations;

namespace anime_downloader.Models
{
    public class AnimeDetails : INotifyPropertyChanged
    {
        private string _resolution;

        private bool _airing;

        private string _status;

        private string _episode;

        private string _rating;

        public string Resolution
        {
            get { return _resolution; }
            set
            {
                if (value == _resolution) return;
                _resolution = value;
                OnPropertyChanged();
            }
        }

        public bool Airing
        {
            get { return _airing; }
            set
            {
                if (value == _airing) return;
                _airing = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        public string Episode
        {
            get { return _episode; }
            set
            {
                if (value == _episode) return;
                _episode = value;
                OnPropertyChanged();
            }
        }

        public string Rating
        {
            get { return _rating; }
            set
            {
                if (value == _rating) return;
                _rating = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
