using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace anime_downloader.Classes {
    public class Nyaa {
        /// <summary>
        ///     A conversion chart from any of these values to megabytes.
        /// </summary>
        private static readonly Dictionary<string, double> toMegabyte = new Dictionary<string, double> {
            {"MiB", 1.04858},
            {"GiB", 1073.74},
            {"KiB", 0.001024}
        };

        /// <summary>
        ///     The description containing seeder & measurement information.
        /// </summary>
        public string description;

        /// <summary>
        ///     The given download link.
        /// </summary>
        public string link;

        /// <summary>
        ///     The unit of measurement used in size.
        /// </summary>
        public string measurement;

        /// <summary>
        ///     The nyaa's parsed filename.
        /// </summary>
        public string name;

        /// <summary>
        ///     The amount of people seeding the torrent.
        /// </summary>
        public int seeders;

        /// <summary>
        ///     The size of the download.
        /// </summary>
        public double size;

        /// <summary>
        ///     HTML Nyaa Initializer
        /// </summary>
        /// <param name="node">A raw node.</param>
        public Nyaa(HtmlNode node) {
            name = WebUtility.HtmlDecode(node.Element("title").InnerText.Replace("Â", ""));
            link = node.Element("#text").InnerText.Replace("#38;", "");
            description = node.Element("description").InnerText;
            if (description.Contains("CDATA"))
                description = description
                    .Split(new[] {"<![CDATA["}, StringSplitOptions.None)[1]
                    .Split(new[] {"]]>"}, StringSplitOptions.None)[0];
            seeders = int.Parse(description.Split(new[] {" seeder"}, StringSplitOptions.None)[0]);
            measurement = toMegabyte.Where(d => description.Contains(d.Key)).First().Key;
            size =
                Math.Round(double.Parse(description.Split(new[] {$" {measurement}"}, StringSplitOptions.None)[0]
                    .Split(new[] {" - "}, StringSplitOptions.None)[1])
                           *toMegabyte[measurement],
                    2);
        }

        /// <summary>
        ///     A simple representation of the important attribes of a Nyaa object.
        /// </summary>
        /// <returns>summary of nyaa values</returns>
        public override string ToString() => $"Nyaa<name={name}, link={link}, size={size} MB>";

        /// <summary>
        ///     Gathers the torrent's filename from it's meta-data.
        /// </summary>
        /// <returns>A valid filename for the torrent.</returns>
        public string torrentName() {
            string filename, disposition;
            var request = (HttpWebRequest) WebRequest.Create(link);

            try {
                var res = (HttpWebResponse) request.GetResponse();
                using (var rstream = res.GetResponseStream()) {
                    disposition = res.Headers["content-disposition"];
                    filename = disposition.Split(new[] {"filename=\""}, StringSplitOptions.None)[1].Split('"')[0];
                }
                res.Close();
                return filename;
            }

            catch {}

            return null;
        }

        /// <summary>
        ///     Strips the filename to remove extraneous information and returns name.
        /// </summary>
        /// <param name="removeEpisode">A flag for also removing the episode number</param>
        /// <returns>The name of the anime.</returns>
        public string strippedName(bool removeEpisode = false) {
            var phrases = new List<string>();
            var text = name;

            foreach (Match match in Regex.Matches(text, @"\s?\[(.*?)\]|\((.*?)\)\s*"))
                phrases.Add(match.Groups[0].Value);

            foreach (var phrase in phrases)
                text = text.Replace(phrase, "");

            if (removeEpisode)
                text = string.Join("-", text.Split('-').Take(text.Split('-').Length - 1).ToArray());

            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        /// <summary>
        ///     Returns the subgroup from the name of the file.
        /// </summary>
        /// <returns>The subgroup of the file.</returns>
        public string subgroup() {
            foreach (Match match in Regex.Matches(name, @"\[([A-Za-z0-9_µ\-]+)\]+")) {
                var result = match.Groups[1].Value;
                if (result.All(c => !char.IsNumber(c)))
                    return result;
            }
            return null;
        }

        /// <summary>
        ///     A check if the subgroup exists.
        /// </summary>
        /// <returns>If a subgroup exists.</returns>
        public bool hasSubgroup() => subgroup() != null;
    }
}