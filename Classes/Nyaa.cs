using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using HtmlAgilityPack;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace anime_downloader.Classes {
    public class Nyaa {

        public string name, link, description, measurement;
        public int seeders;
        public double size;

        private static readonly Dictionary<string, double> toMegabyte = new Dictionary<string, double> {
            { "MiB", 1.04858  },
            { "GiB", 1073.74  },
            { "KiB", 0.001024 }
        };

        public Nyaa(HtmlNode node) {
            name = node.Element("title").InnerText;
            link = node.Element("#text").InnerText.Replace("#38;", "");
            description = node.Element("description").InnerText;
            if (description.Contains("CDATA"))
                description = description
                                        .Split(new string[] {"<![CDATA["}, StringSplitOptions.None)[1]
                                        .Split(new string[] {"]]>"}, StringSplitOptions.None)[0];
            seeders = int.Parse(description.Split(new string[] {" seeder"}, StringSplitOptions.None)[0]);
            measurement = toMegabyte.Where(d => description.Contains(d.Key)).First().Key;
            size =
                Math.Round(double.Parse(description.Split(new string[] {$" {measurement}"}, StringSplitOptions.None)[0]
                    .Split(new string[] {" - "}, StringSplitOptions.None)[1])
                           *toMegabyte[measurement],
                    2);
        }

        public override string ToString() => $"Nyaa<name={name}, link={link}, size={size} MB>";

        public string torrentName() {
            string filename, disposition;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
            try {

                HttpWebResponse res = (HttpWebResponse)request.GetResponse();
                using (Stream rstream = res.GetResponseStream()) {
                    disposition = res.Headers["content-disposition"];
                    filename = disposition.Split(new string[] { "filename=\"" }, StringSplitOptions.None)[1].Split('"')[0];
                    /*
                    string fileName = res.Headers["Content-Disposition"] != null ?
                        res.Headers["Content-Disposition"].Replace("attachment; filename=", "").Replace("\"", "") :
                        res.Headers["Location"] != null ? Path.GetFileName(res.Headers["Location"]) :
                        Path.GetFileName(url).Contains('?') || Path.GetFileName(url).Contains('=') ?
                        Path.GetFileName(res.ResponseUri.ToString()) : defaultFileName;
                        */
                }
                res.Close();
                return filename;

            }
            catch { }

            return null;
            

            /*
            string disposition, filename;
            using (WebClient client = new WebClient()) {
                /*
                client.OpenReadCompleted += (object sender, OpenReadCompletedEventArgs e) => {
                    var disposition = client.ResponseHeaders["content-disposition"];
                    torrent = disposition.Split(new string[] {"filename=\""}, StringSplitOptions.None)[1].Split('"')[0];
                };
                Task.Run(() => client.OpenReadAsync(new Uri(link))).Wait();
                */
            /*
            client.OpenRead(link);
            disposition = client.ResponseHeaders["content-disposition"];
            filename = disposition.Split(new string[] { "filename=\"" }, StringSplitOptions.None)[1].Split('"')[0];
        }
        return filename;
        */
        }
        

        public string strippedName(bool removeEpisode=false) {
            List<string> phrases = new List<string>();
            string text = name;

            foreach (Match match in Regex.Matches(text, @"\s *\[(.*?)\]|\((.*?)\)\s*"))
                phrases.Add(match.Groups[0].Value);
            foreach(String phrase in phrases)
                text = text.Replace(phrase, "");
            if (removeEpisode)
                text = String.Join("-", text.Split('-').Take(text.Split('-').Length-1).ToArray());
             
            return Regex.Replace(text.Trim(), @"\s+", " ");
            
        }

        public string subgroup() {
            foreach (Match match in Regex.Matches(name, @"\[([A-Za-z0-9_]+)\]+")) {
                var result = match.Groups[1].Value;
                if (result.All(c => !Char.IsNumber(c)))
                    return result;
            }
            return null;
        }

        public bool hasSubgroup() => subgroup() != null;

    }
}