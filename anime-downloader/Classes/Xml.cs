using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace anime_downloader.Classes {
    public class Xml {
        private readonly Settings _settings;

        public Xml(Settings settings) {
            _settings = settings;
        }

        /// <summary>
        ///     Kind of a lazy way to test XML changes between versions.
        /// </summary>
        public static void VerifySettingsXmlSchema(string settingsXmlPath) {
            var document = XDocument.Load(settingsXmlPath);
            var root = document.Root;

            if (root?.Element("sortBy") == null)
                root?.Add(new XElement("sortBy", "name"));

            document.Save(settingsXmlPath);
        }

        /// <summary>
        ///     Kind of a lazy way to test XML changes between versions.
        /// </summary>
        public static void VerifyAnimeXmlSchema(string animeXmlPath) {
            var document = XDocument.Load(animeXmlPath);
            var root = document.Root;

            if (root != null) {
                foreach (var anime in root.Elements().Where(anime => anime.Element("preferredSubgroup") == null)) {
                    anime.Add(new XElement("preferredSubgroup", ""));
                }
            }

            document.Save(animeXmlPath);
        }

        /// <summary>
        ///     Create the Anime XML file with initial nodes.
        /// </summary>
        public void CreateAnimeXml() {
            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("The anime list."),
                    new XElement("anime")
                    );
            document.Save(_settings.AnimeXmlPath);
        }

        /// <summary>
        ///     Add a single anime into the anime XML file.
        /// </summary>
        /// <param name="anime">A valid anime object.</param>
        public void AddAnime(Anime anime) {
            var document = XDocument.Load(_settings.AnimeXmlPath);

            var element = new XElement("show",
                new XElement("name", anime.Name),
                new XElement("episode", anime.Episode),
                new XElement("status", anime.Status),
                new XElement("resolution", anime.Resolution),
                new XElement("airing", anime.Airing),
                new XElement("updated", false),
                new XElement("name-strict", anime.NameStrict),
                new XElement("preferredSubgroup", anime.PreferredSubgroup),
                new XElement("last-downloaded", "2016-02-04"));

            document.Element("anime")?.Add(element);
            document.Save(_settings.AnimeXmlPath);
        }

        /// <summary>
        ///     Add multiple animes into the anime XML file.
        /// </summary>
        /// <param name="animes">A collection of valid animes.</param>
        public void AddAnime(List<Anime> animes) {
            var document = XDocument.Load(_settings.AnimeXmlPath);

            foreach (var anime in animes) {
                var element = new XElement("show",
                    new XElement("name", anime.Name),
                    new XElement("episode", anime.Episode),
                    new XElement("status", anime.Status),
                    new XElement("resolution", anime.Resolution),
                    new XElement("airing", anime.Airing),
                    new XElement("updated", false),
                    new XElement("name-strict", anime.NameStrict),
                    new XElement("preferredSubgroup", anime.PreferredSubgroup),
                    new XElement("last-downloaded", "2016-02-04"));
                document.Element("anime")?.Add(element);
            }

            document.Save(_settings.AnimeXmlPath);
        }

        /// <summary>
        ///     Edit a single anime and write that change to the anime XML file.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        /// <param name="anime">The replacing anime object.</param>
        public void EditAnime(string name, Anime anime) {
            var document = XDocument.Load(_settings.AnimeXmlPath);

            var selected = document.Root?.Elements()
                .Where(a => a.Element("name")?.Value.Equals(name) ?? false)
                .FirstOrDefault();

            if (selected == null || anime == null)
                return;

            selected.SetElementValue("name", anime.Name);
            selected.SetElementValue("episode", anime.Episode);
            selected.SetElementValue("status", anime.Status);
            selected.SetElementValue("resolution", anime.Resolution);
            selected.SetElementValue("airing", anime.Airing.ToString());
            selected.SetElementValue("name-strict", anime.NameStrict);
            selected.SetElementValue("preferredSubgroup", anime.PreferredSubgroup);
            document.Save(_settings.AnimeXmlPath);
        }

        /// <summary>
        ///     Edit a specific elementName about an anime instead of needing an entire object.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        /// <param name="elementName">The identifying element name.</param>
        /// <param name="elementValue">The value to be written to the element.</param>
        public void EditAnime(string name, string elementName, string elementValue) {
            var document = XDocument.Load(_settings.AnimeXmlPath);

            var selected = document.Root?.Elements()
                .Where(a => a.Element("name")?.Value.Equals(name) ?? false)
                .FirstOrDefault();
            var element = selected?.Element(elementName);
            if (element != null) {
                element.Value = elementValue;
                document.Save(_settings.AnimeXmlPath);
            }
        }

        /// <summary>
        ///     Edit multiple anime and write those changes to the anime XML file.
        /// </summary>
        /// <remarks>This works under the assumption that all Anime "name" are not modified.</remarks>
        /// <param name="animes">A collection of valid anime objects.</param>
        public void EditAnime(IEnumerable<Anime> animes) {
            var document = XDocument.Load(_settings.AnimeXmlPath);

            if (animes == null)
                return;

            foreach (var anime in animes) {
                if (anime == null)
                    continue;

                var selected = document.Root?.Elements()
                    .Where(a => a.Element("name")?.Value.Equals(anime.Name) ?? false)
                    .FirstOrDefault();

                if (selected == null)
                    continue;

                selected.SetElementValue("name", anime.Name);
                selected.SetElementValue("episode", anime.Episode);
                selected.SetElementValue("status", anime.Status);
                selected.SetElementValue("resolution", anime.Resolution);
                selected.SetElementValue("airing", anime.Airing.ToString());
                selected.SetElementValue("name-strict", anime.NameStrict.ToString());
                selected.SetElementValue("preferredSubgroup", anime.PreferredSubgroup);
            }

            document.Save(_settings.AnimeXmlPath);
        }

        /// <summary>
        ///     Remove a single anime from the anime XML file.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        public void RemoveAnime(string name) {
            var document = XDocument.Load(_settings.AnimeXmlPath);
            var node = document.Root?.Elements().Where(x => x.Element("name")?.Value == name).FirstOrDefault();

            if (node == null)
                return;

            node.Remove();
            document.Save(_settings.AnimeXmlPath);
        }

        /// <summary>
        ///     Create a new XML file and populate it with the values from a settings object.
        /// </summary>
        /// <param name="settings">A valid instantiated settings object.</param>
        public void CreateSettingsXml(Settings settings) {
            var subgroups = new XElement("subgroup");
            foreach (var group in settings.Subgroups)
                subgroups.Add(new XElement("name", group));

            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("User profile settings"),
                    new XElement("settings",
                        new XElement("name", Environment.UserName),
                        new XElement("path",
                            new XElement("base", settings.BaseFolderPath),
                            new XElement("torrents", settings.TorrentFilesPath),
                            new XElement("utorrent", settings.UtorrentPath)),
                        subgroups,
                        new XElement("flag",
                            new XElement("only-whitelisted-subs", settings.OnlyWhitelisted))),
                    new XElement("sortBy", settings.SortBy)
                    );

            document.Save(settings.SettingsXmlPath);
        }

        // implement
        public void EditSettingsXml(Settings settings) {}
    }
}