using System;
using System.Xml.Linq;

namespace anime_downloader.Classes.Xml {
    public class XmlCreate {
        private readonly Settings _settings;

        public XmlCreate(Settings settings) {
            _settings = settings;
        }

        /// <summary>
        ///     Create the Anime XML file with initial nodes.
        /// </summary>
        public void AnimeXml() {
            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("The anime list."),
                    new XElement("anime")
                );
            document.Save(_settings.AnimeXmlPath);
        }

        /// <summary>
        ///     Create a new XML file and populate it with the values from a settings object.
        /// </summary>
        public void SettingsXml() {
            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("User profile settings"),
                    new XElement("settings",
                        new XElement("name", Environment.UserName),
                        new XElement("path",
                            new XElement("base"),
                            new XElement("torrents"),
                            new XElement("utorrent")
                        ),
                        new XElement("subgroup"),
                        new XElement("flag",
                            new XElement("only-whitelisted-subs")
                            ),
                    new XElement("sortBy")
                    )
                );

            document.Save(_settings.SettingsXmlPath);
        }
    }
}
