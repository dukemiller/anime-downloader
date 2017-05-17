using System.Collections.Generic;
using anime_downloader.Models.Abstract;

namespace anime_downloader.Models
{
    public class MagnetLink: RemoteMedia
    {
        /// <summary>
        ///     The hashcode associated with the magnet.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        ///     The collection of trackers
        /// </summary>
        public List<string> Trackers { get; set; } = new List<string>();

        /// <summary>
        ///     The explicit (if given) name of the resulting downloaded file.
        /// </summary>
        public string DirectName { get; set; }
    }
}
