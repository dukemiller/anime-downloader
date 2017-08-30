using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using anime_downloader.Patch.Services.Interface;
using Newtonsoft.Json;

namespace anime_downloader.Patch.Services
{
    public class PatchService : IPatchService
    {
        public static string ApplicationDirectory => Path.Combine(Environment
                .GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "anime_downloader");

        // 

        private Version Previous { get; set; }

        private Version Current { get; set; }

        private List<string> OldFiles { get; set; } = new List<string>();

        // 

        public Action<string> Output { get; set; } = Console.WriteLine;

        public (bool updated, bool failed) Patch(Version previous, Version current)
        {
            Previous = previous;
            Current = current;

            if (Previous.Major == 0 && Current.Major == 1)
            {
                if (File.Exists(Path.Combine(ApplicationDirectory, "settings.json")))
                {
                    Output("Settings.json already found, stopping.");
                    return (false, false);
                }

                else
                {
                    Output($"Updating from {previous} to {current}.");

                    var (updated, failed) = Version_1_x_x();

                    if (failed)
                    {
                        Output("Something went wrong. Restoring backup.");
                        var restored = RestoreBackup();
                        if (!restored)
                            Output("Backup corrupted?");
                        return (false, false);
                    }

                    else
                    {
                        Output("Update complete.");
                        return (true, false);
                    }
                }
            }

            else
            {
                Output("Up to date.");
                return (false, false);
            }
        }

        // General

        private bool BackupSettings()
        {
            Output("-- Backing up settings.");

            var path = Path.Combine(ApplicationDirectory, $"{Previous}.zip");

            if (File.Exists(path))
                return true;

            try
            {
                var extensions = new[] {".xml", ".json", ".txt"};

                OldFiles = Directory
                    .GetFiles(ApplicationDirectory)
                    .Where(file => extensions.Any(ext => file.ToLower().EndsWith(ext)))
                    .ToList();

                using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
                {
                    foreach (var file in OldFiles)
                        zip.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Fastest);
                    //zip.AlternateEncoding = Encoding.UTF8;
                    //zip.Save(path);
                }

                return true;
            }

            catch (Exception e)
            {
                Output($"An error occured.{e}\n");
                return false;
            }
        }

        private bool RestoreBackup()
        {
            var path = Path.Combine(ApplicationDirectory, $"{Previous}.zip");

            try
            {
                ZipFile.ExtractToDirectory(path, ApplicationDirectory);
                File.Delete(path);
                return true;
            }

            catch
            {
                return false;
            }
        }

        // Version 1.x.x. specific methods

        /// <summary>
        ///     split settings.xml to multiple .json files
        /// </summary>
        private (bool updated, bool failed) Version_1_x_x()
        {
            var settings = Path.Combine(ApplicationDirectory, "settings.xml");

            if (!File.Exists(settings))
            {
                Output("No settings.xml found, stopping.");
                return (false, false);
            }

            var backedup = BackupSettings();

            if (!backedup)
                return (false, true);

            var doc = new XmlDocument();
            doc.Load(settings);

            var main = doc.SelectSingleNode("Settings");

            var anime = main?.SelectSingleNode("Animes");
            var mal = main?.SelectSingleNode("MyAnimeList");
            var anilist = main?.SelectSingleNode("AniList");
            var credentials = main?.SelectSingleNode("AniList/Credentials");

            if (anime != null && mal != null && credentials != null && anilist != null)
            {
                main.RemoveChild(anime);
                main.RemoveChild(mal);
                main.RemoveChild(anilist);

                var malnode = XElement.Load(mal.CreateNavigator().ReadSubtree());
                malnode.Add(new XElement("api",
                    new XElement("cookies", ""),
                    new XElement("csrf_token", ""),
                    new XElement("csrf_token_last_retrieved", DateTime.MinValue)
                ));

                var xml = new XDocument(
                    new XElement("xml",
                        malnode,
                        new XElement("anilist",
                            XElement.Load(credentials.CreateNavigator().ReadSubtree()))
                    )
                );

                var newdoc = new XmlDocument();
                newdoc.LoadXml(xml.ToString());

                // Create the new files
                try
                {
                    CreateSettingsJson(main);
                    CreateAnimeJson(anime);
                    CreateCredentialsJson(newdoc);
                }

                catch
                {
                    return (false, true);
                }

                // Rename old anilist
                Output("-- Renaming anilist.json -> airing_shows.json");
                var airing = Path.Combine(ApplicationDirectory, "anilist.json");
                if (File.Exists(airing))
                    File.Move(airing, Path.Combine(ApplicationDirectory, "airing_shows.json"));

                // Remove the previous files
                Output("-- Removing old files.");
                foreach (var file in OldFiles)
                    File.Delete(file);

                if (File.Exists(settings))
                    File.Delete(settings);

                return (true, false);
            }

            return (false, true);
        }

        private void CreateSettingsJson(XmlNode main)
        {
            Output("-- Creating settings.json");

            var json = JsonConvert.SerializeXmlNode(main, Newtonsoft.Json.Formatting.Indented, true).Replace("@", "");

            json = string.Join("\n", json.Split('\n').Skip(3));
            json = "{\n" + json;
            json = json.Replace("\"false\"", "false");
            json = json.Replace("\"true\"", "true");
            json = json.Replace("\"Paths\"", "\"paths\"");
            json = json.Replace("\"Flags\"", "\"flags\"");
            json = json.Replace("\"Version\"", "\"version\"");
            json = json.Replace("\"Provider\"", "\"provider\"");
            json = json.Replace("\"SortBy\"", "\"sort_by\"");
            json = json.Replace("\"FilterBy\"", "\"filter_by\"");
            json = json.Replace("\"NyaaPantsu\"", "0");
            json = json.Replace("\"NyaaSi\"", "1");
            json = json.Replace("\"HorribleSubs\"", "2");

            json = json.Replace("\"Subgroups\": {", "\"subgroups\": [");
            json = Regex.Replace(json, @"(^\s+""string"": .*)", "", RegexOptions.Multiline);
            json = json.Replace("\"Subgroups\": {", "\"subgroups\": [");
            json = Regex.Replace(json, @"\n+", "\n");
            json = Regex.Replace(json, @"      ", "    ");
            json = Regex.Replace(json, @"    ]\r\n\s+}", "  ]", RegexOptions.Multiline);

            File.WriteAllText(Path.Combine(ApplicationDirectory, "settings.json"), json);
        }

        private void CreateCredentialsJson(XmlNode main)
        {
            Output("-- Creating credentials.json");

            var json = JsonConvert.SerializeXmlNode(main, Newtonsoft.Json.Formatting.Indented, true).Replace("@", "");
            json = json.Replace("\"MyAnimeList\"", "\"myanimelist\"");
            json = json.Replace("\"AccessToken\"", "\"access_token\"");
            json = json.Replace("\"TokenType\"", "\"token_type\"");
            json = json.Replace("\"Expires\"", "\"expires\"");
            json = json.Replace("\"ExpiresIn\"", "\"expires_in\"");
            json = json.Replace("\"Credentials\"", "\"api\"");
            json = json.Replace("\"false\"", "false");
            json = json.Replace("\"true\"", "true");

            File.WriteAllText(Path.Combine(ApplicationDirectory, "credentials.json"), json);
        }

        private void CreateAnimeJson(XmlNode anime)
        {
            Output("-- Creating anime.json");

            var json = JsonConvert.SerializeXmlNode(anime, Newtonsoft.Json.Formatting.Indented, true).Replace("@", "")
                .Replace("my_anime_list", "details");

            json = json.Replace("\"false\"", "false");
            json = json.Replace("\"true\"", "true");
            json = json.Replace("\"Anime\"", "\"anime\"");
            json = json.Replace("\"Watching\"", "0");
            json = json.Replace("\"Considering\"", "1");
            json = json.Replace("\"Finished\"", "2");
            json = json.Replace("\"On Hold\"", "3");
            json = json.Replace("\"Dropped\"", "4");
            json = json.Replace("\"Year\"", "\"year\"");
            json = json.Replace("\"Season\"", "\"season\"");
            json = json.Replace("\"Winter\"", "1");
            json = json.Replace("\"Spring\"", "2");
            json = json.Replace("\"Summer\"", "3");
            json = json.Replace("\"Fall\"", "4");

            foreach (var match in Regex.Matches(json, @"\s+\""(?!resolution|id|rating)\w+\"": (\""(\d+)\""),")
                .Cast<Match>().Distinct())
            {
                var phrase = match.Groups[0].Value.Replace(match.Groups[1].Value, match.Groups[2].Value);
                json = Regex.Replace(json, match.Groups[0].Value, phrase);
            }

            File.WriteAllText(Path.Combine(ApplicationDirectory, "anime.json"), json);
        }
    }
}