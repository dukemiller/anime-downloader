using anime_downloader.Models;

namespace anime_downloader.Classes.File
{
    public class MovedAnimeFile
    {
        public AnimeFile Old { get; set; }
        public AnimeFile Latest { get; set; }
    }
}