using System;
using System.Xml.Linq;

namespace anime_downloader.Classes.Xml
{
    /// <summary>
    ///     The purpose of this class is to keep the schema for any nodes or documents
    ///     in one location.
    /// </summary>
    public class XmlCreate
    {
        private readonly Settings _settings;

        public XmlCreate(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        ///     Create the anime xml file with initial nodes.
        /// </summary>
        /// <returns></returns>
        public static XDocument AnimeXml()
        {
            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("The anime list."),
                    new XElement("anime")
                    );
            return document;
        }

        /// <summary>
        ///     Create an anime node.
        /// </summary>
        /// <returns></returns>
        public static XElement AnimeNode()
        {
            var node = new XElement("show",
                new XElement("name"),
                new XElement("episode", "00"),
                new XElement("status", "Watching"),
                new XElement("resolution", "720"),
                new XElement("airing", false),
                new XElement("updated", false),
                new XElement("name-strict", false),
                new XElement("preferredSubgroup"),
                new XElement("last-downloaded", "2016-02-04"),
                new XElement("rating", "")
                );
            return node;
        }

        /// <summary>
        ///     Create the settings xml file with initial nodes.
        /// </summary>
        /// <returns></returns>
        public static XDocument SettingsXml()
        {
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
                            new XElement("only-whitelisted-subs"),
                            new XElement("use-logging")
                            ),
                        new XElement("sortBy"),
                        new XElement("filterBy")
                        // ,new XElement("idont")
                        )
                    );
            return document;
        }

        /// <summary>
        ///     Create the settings xml file with initial nodes and save to settings-defined xml location.
        /// </summary>
        public void SettingsXmlAndSave()
        {
            var document = SettingsXml();
            document.Save(_settings.SettingsXmlPath);
        }

        /// <summary>
        ///     Create the anime xml file with initial nodes and save to settings-defined xml location.
        /// </summary>
        public void AnimeXmlAndSave()
        {
            var document = AnimeXml();
            document.Save(_settings.AnimeXmlPath);
        }
    }
}