using System;
using System.Xml.Linq;

namespace anime_downloader.Classes.Xml
{
    /// <summary>
    ///     The purpose of this class is to keep the schema for any nodes or documents
    ///     in one location. Any change in the schema here is verified in the Verify
    ///     class and automatically updated and populated with the default given value
    ///     or properly removed.
    /// </summary>
    public static class Schema
    {
        /// <summary>
        ///     Create the anime xml file with initial nodes.
        /// </summary>
        public static XDocument AnimeDocument()
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
        public static XElement AnimeNode()
        {
            var node = new XElement("show",
                new XElement("name"),
                new XElement("episode", "00"),
                new XElement("status", "Watching"),
                new XElement("resolution", "720"),
                new XElement("airing", false),
                new XElement("name-strict", false),
                new XElement("preferredSubgroup"),
                new XElement("rating")
                );
            return node;
        }

        /// <summary>
        ///     Create the settings xml file with initial nodes.
        /// </summary>
        public static XDocument SettingsDocument()
        {
            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("User profile settings"),
                    new XElement("settings",
                        new XElement("name", Environment.UserName),
                        new XElement("path",
                            new XElement("torrents"),
                            new XElement("utorrent"),
                            new XElement("watched"),
                            new XElement("episode")
                            ),
                        new XElement("subgroup"),
                        new XElement("flag",
                            new XElement("individualShowFolders", false),
                            new XElement("onlyWhitelistedSubs", false),
                            new XElement("exitOnClose", true),
                            new XElement("alwaysShowTray", false),
                            new XElement("sortByReversed", false)
                            ),
                        new XElement("sortBy", "name"),
                        new XElement("filterBy")
                        )
                    );
            return document;
        }

        /// <summary>
        ///     Create the settings xml file with initial nodes and save to settings-defined xml location.
        /// </summary>
        public static void CreateSettingsXml()
        {
            var document = SettingsDocument();
            document.Save(Settings.SettingsXml);
        }

        /// <summary>
        ///     Create the anime xml file with initial nodes and save to settings-defined xml location.
        /// </summary>
        public static void CreateAnimeXml()
        {
            var document = AnimeDocument();
            document.Save(Settings.AnimeXml);
        }
    }
}