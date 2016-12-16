using System.Collections.Generic;
using anime_downloader.Models;

namespace anime_downloader.Classes.File
{
    public class MultipleAnimeFiles
    {
        public Anime Anime { get; set; }

        public IEnumerable<AnimeFile> Episodes { get; set; }
    }
}