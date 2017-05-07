using System;
using System.Net;
using System.Threading.Tasks;
using anime_downloader.Models.Abstract;

namespace anime_downloader.Models
{
    public class Torrent: RemoteMedia
    {
        /// <summary>
        ///     The description containing seeder & measurement information.
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        ///     The unit of measurement used in size.
        /// </summary>
        public string Measurement { get; set; }

        /// <summary>
        ///     The amount of people seeding the torrent.
        /// </summary>
        public int Seeders { get; set; }

        /// <summary>
        ///     The size of the download.
        /// </summary>
        public double Size { get; set; }

        //

        /// <summary>
        ///     A representation of the important attributes of a Nyaa object.
        /// </summary>
        public override string ToString() => $"{GetType().Name}<name={Name}, link={Remote}, size={Size} MB>";

        // 

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

            var request = (HttpWebRequest) WebRequest.Create(Remote);
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

    }
}