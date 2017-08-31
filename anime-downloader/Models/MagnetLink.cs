using System.Collections.Generic;
using anime_downloader.Models.Abstract;

namespace anime_downloader.Models
{
    public class MagnetLink: RemoteMedia
    {
        private int _seeders;

        /// <summary>
        ///     The hashcode associated with the magnet.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        ///     The collection of trackers
        /// </summary>
        public List<string> Trackers { get; set; } = new List<string>();

        /// <summary>
        ///     Seeder information.
        /// </summary>
        public int Seeders
        {
            get => _seeders;
            set
            {
                _seeders = value;
                Health = value;
            }
        }

        /// <summary>
        ///     The explicit (if given) name of the resulting downloaded file.
        /// </summary>
        public string DirectName { get; set; }

        public override string ToString()
        {
            return DirectName != null
                ? $"MagnetLink<name={DirectName}, hash={Hash}>"
                : $"MagnetLink<name={Name}, hash={Hash}>";
        }
    }
}
