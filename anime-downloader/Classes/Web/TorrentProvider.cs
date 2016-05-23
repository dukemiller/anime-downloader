using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace anime_downloader.Classes.Web
{
    public abstract class TorrentProvider
    {
        /// <summary>
        ///     The description containing seeder & measurement information.
        /// </summary>
        public string Description;

        /// <summary>
        ///     The given download link.
        /// </summary>
        public string Link;

        /// <summary>
        ///     The unit of measurement used in size.
        /// </summary>
        public string Measurement;

        /// <summary>
        ///     The TorrentProvider's parsed filename.
        /// </summary>
        public string Name;

        /// <summary>
        ///     The amount of people seeding the torrent.
        /// </summary>
        public int Seeders;

        /// <summary>
        ///     The size of the download.
        /// </summary>
        public double Size;

        /// <summary>
        ///     A simple representation of the important attribes of a Nyaa object.
        /// </summary>
        /// <returns>
        ///     Summary of torrent providers' values
        /// </returns>
        public override string ToString() => $"{GetType().Name}<name={Name}, link={Link}, size={Size} MB>";

        /// <summary>
        ///     Gathers the torrent's filename from it's meta-data.
        /// </summary>
        /// <returns>
        ///     A valid filename for the torrent.
        /// </returns>
        /// <remarks>
        ///     This is only known to be the case for Nyaa.EU's torrents
        /// </remarks>
        public async Task<string> GetTorrentNameAsync()
        {
            HttpWebResponse response = null;

            var request = (HttpWebRequest) WebRequest.Create(Link);
            request.Timeout = 3000;
            request.AllowAutoRedirect = false;
            request.Method = "HEAD";

            try
            {
                response = (HttpWebResponse) await request.GetResponseAsync();
                var disposition = response.Headers["content-disposition"];
                var filename = disposition?.Split(new[] {"filename=\""}, StringSplitOptions.None)[1].Split('"')[0];
                return filename;
            }

            catch (Exception ex) when (ex is WebException || ex is InvalidOperationException)
            {
                return null;
            }

            finally
            {
                response?.Close();
            }
        }

        /// <summary>
        ///     Strips the filename to remove extraneous information and returns name.
        /// </summary>
        /// <param name="removeEpisode">
        ///     A flag for also removing the episode number
        /// </param>
        /// <returns>
        ///     The name of the anime.
        /// </returns>
        public string StrippedName(bool removeEpisode = false)
        {
            var text = Name;

            var phrases = (from Match match in Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*")
                select match.Groups[0].Value).ToList();

            phrases.ForEach(p => text = text.Replace(p, ""));
            text = text.Replace("_", " ");
            text = new[] {".mkv", ".mp4", ".avi"}.Aggregate(text, (current, s) => current.Replace(s, ""));

            if (removeEpisode)
                text = string.Join("-", text.Split('-').Take(text.Split('-').Length - 1).ToArray());

            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        ///     Returns the subgroup from the name of the file.
        /// </summary>
        /// <returns>The subgroup of the file.</returns>
        public string Subgroup()
        {
            return (from Match match in Regex.Matches(Name, @"\[([A-Za-z0-9_µ\s\-]+)\]+")
                select match.Groups[1].Value).FirstOrDefault(result => result.All(c => !char.IsNumber(c)));
        }

        /// <summary>
        ///     A check if the subgroup exists.
        /// </summary>
        /// <returns>If a subgroup exists.</returns>
        public bool HasSubgroup() => Subgroup() != null;

        // public abstract Task<IEnumerable<TorrentProvider>> GetTorrentsFor(Anime anime, string episode);
    }
}