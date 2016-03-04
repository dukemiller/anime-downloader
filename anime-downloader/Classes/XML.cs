using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace anime_downloader.Classes {
    public class XML {
        private readonly Settings settings;

        public XML(Settings settings) {
            this.settings = settings;
        }

        /// <summary>
        ///     Kind of a lazy way to test XML changes between versions.
        /// </summary>
        public static void verifySettingsXMLSchema(string settingsXMLPath) {
            var document = XDocument.Load(settingsXMLPath);
            var root = document.Root;

            if (root.Element("sortBy") == null)
                root.Add(new XElement("sortBy", "name"));

            document.Save(settingsXMLPath);
        }

        /// <summary>
        ///     Kind of a lazy way to test XML changes between versions.
        /// </summary>
        public static void verifyAnimeXMLSchema(string animeXMLPath) {
            var document = XDocument.Load(animeXMLPath);
            var root = document.Root;

            foreach (var anime in root.Elements()) {
                if (anime.Element("preferredSubgroup") == null)
                    anime.Add(new XElement("preferredSubgroup", ""));
            }

            document.Save(animeXMLPath);
        }

        /// <summary>
        ///     Create the Anime XML file with initial nodes.
        /// </summary>
        public void createAnimeXML() {
            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("The anime list."),
                    new XElement("anime")
                    );
            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        ///     Add a single anime into the anime XML file.
        /// </summary>
        /// <param name="anime">A valid anime object.</param>
        public void addAnime(Anime anime) {
            var document = XDocument.Load(settings.animeXMLPath);

            var element = new XElement("show",
                new XElement("name", anime.name),
                new XElement("episode", anime.episode),
                new XElement("status", anime.status),
                new XElement("resolution", anime.resolution),
                new XElement("airing", anime.airing),
                new XElement("updated", false),
                new XElement("name-strict", anime.nameStrict),
                new XElement("preferredSubgroup", anime.preferredSubgroup),
                new XElement("last-downloaded", "2016-02-04"));

            document.Element("anime").Add(element);
            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        ///     Add multiple animes into the anime XML file.
        /// </summary>
        /// <param name="animes">A collection of valid animes.</param>
        public void addAnime(List<Anime> animes) {
            var document = XDocument.Load(settings.animeXMLPath);

            foreach (var anime in animes) {
                var element = new XElement("show",
                    new XElement("name", anime.name),
                    new XElement("episode", anime.episode),
                    new XElement("status", anime.status),
                    new XElement("resolution", anime.resolution),
                    new XElement("airing", anime.airing),
                    new XElement("updated", false),
                    new XElement("name-strict", anime.nameStrict),
                    new XElement("preferredSubgroup", anime.preferredSubgroup),
                    new XElement("last-downloaded", "2016-02-04"));
                document.Element("anime").Add(element);
            }

            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        ///     Edit a single anime and write that change to the anime XML file.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        /// <param name="anime">The replacing anime object.</param>
        public void editAnime(string name, Anime anime) {
            var document = XDocument.Load(settings.animeXMLPath);
            var root = document.Root;

            var selected = root.Elements()
                .AsParallel()
                .Where(a => a.Element("name").Value.Equals(name))
                .FirstOrDefault();

            if (selected != null) {
                selected.Element("name").Value = anime.name;
                selected.Element("episode").Value = anime.episode;
                selected.Element("status").Value = anime.status;
                selected.Element("resolution").Value = anime.resolution;
                selected.Element("airing").Value = anime.airing.ToString();
                selected.Element("name-strict").Value = anime.nameStrict.ToString();
                selected.Element("preferredSubgroup").Value = anime.preferredSubgroup;
                document.Save(settings.animeXMLPath);
            }
        }

        /// <summary>
        ///     Edit a specific elementName about an anime instead of needing an entire object.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        /// <param name="elementName">The identifying element name.</param>
        /// <param name="elementValue">The value to be written to the element.</param>
        public void editAnime(string name, string elementName, string elementValue) {
            var document = XDocument.Load(settings.animeXMLPath);
            var root = document.Root;

            var selected = root.Elements()
                .AsParallel()
                .Where(a => a.Element("name").Value.Equals(name))
                .FirstOrDefault();

            if (selected != null) {
                var element = selected.Element(elementName);
                if (element != null) {
                    element.Value = elementValue;
                    document.Save(settings.animeXMLPath);
                }
            }
        }

        /// <summary>
        ///     Edit multiple anime and write those changes to the anime XML file.
        /// </summary>
        /// <remarks>This works under the assumption that all Anime "name" are not modified.</remarks>
        /// <param name="animes">A collection of valid anime objects.</param>
        public void editAnime(List<Anime> animes) {
            var document = XDocument.Load(settings.animeXMLPath);
            var root = document.Root;

            foreach (var anime in animes) {
                var selected = root.Elements()
                    .AsParallel()
                    .Where(a => a.Element("name").Value.Equals(anime.name))
                    .FirstOrDefault();

                if (selected != null) {
                    selected.Element("name").Value = anime.name;
                    selected.Element("episode").Value = anime.episode;
                    selected.Element("status").Value = anime.status;
                    selected.Element("resolution").Value = anime.resolution;
                    selected.Element("airing").Value = anime.airing.ToString();
                    selected.Element("name-strict").Value = anime.nameStrict.ToString();
                    selected.Element("preferredSubgroup").Value = anime.preferredSubgroup;
                }
            }

            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        ///     Remove a single anime from the anime XML file.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        public void removeAnime(string name) {
            var document = XDocument.Load(settings.animeXMLPath);
            var node = document.Root.Elements().Where(x => x.Element("name").Value == name).FirstOrDefault();
            if (node != null) {
                node.Remove();
                document.Save(settings.animeXMLPath);
            }
        }

        /// <summary>
        ///     Create a new XML file and populate it with the values from a settings object.
        /// </summary>
        /// <param name="settings">A valid instantiated settings object.</param>
        public void createSettingsXML(Settings settings) {
            var subgroups = new XElement("subgroup");
            foreach (var group in settings.subgroups)
                subgroups.Add(new XElement("name", group));

            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("User profile settings"),
                    new XElement("settings",
                        new XElement("name", Environment.UserName),
                        new XElement("path",
                            new XElement("base", settings.baseFolderPath),
                            new XElement("torrents", settings.torrentFilesPath),
                            new XElement("utorrent", settings.utorrentPath)),
                        subgroups,
                        new XElement("flag",
                            new XElement("only-whitelisted-subs", settings.onlyWhitelisted))),
                    new XElement("sortBy", settings.sortBy)
                    );

            document.Save(settings.settingsXMLPath);
        }

        // implement
        public void editSettingsXML(Settings settings) {}
    }
}