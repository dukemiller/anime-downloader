using anime_downloader.Enums;
using GalaSoft.MvvmLight;

namespace anime_downloader.Models
{
    public class MultipleAnimeDetails : ObservableObject
    {
        public string Resolution { get; set; } = "";

        public bool Airing { get; set; }

        public Status Status { get; set; }

        public string Episode { get; set; } = "";

        public string Rating { get; set; } = "";
    }
}