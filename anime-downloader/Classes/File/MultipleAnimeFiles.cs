using System.Collections.Generic;

namespace anime_downloader.Classes.File
{
    public class MultipleAnimeFiles
    {
        public Anime Anime { get; set; }

        public IEnumerable<AnimeFile> Episodes { get; set; }
    }
}