using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace anime_downloader.Classes.Xml
{
    /// <summary>
    ///     The purpose for this class to ensure that the schema gets updated
    ///     and populated with initial nodes if there is a change in schema for any
    ///     updates in features.
    /// </summary>
    public static class Verify
    {
        /// <summary>
        ///     Compares the current schema to the default schema and adds any elements
        /// </summary>
        private static void Compare(XContainer current, XContainer schema)
        {
            if (current == null || schema == null)
                return;

            // add
            foreach (var element in schema.Elements())
            {
                if (!current.Elements().Any(e => e.Name.Equals(element.Name)))
                    current.Add(new XElement(element.Name, element.Value));

                if (element.HasElements)
                    Compare(current.Element(element.Name.ToString()), element);
            }

            var remove = new List<XElement>();

            // remove
            foreach (var element in current.Elements())
            {
                if (!schema.Elements().Any(e => e.Name.Equals(element.Name)))
                    remove.Add(element);
            }

            remove.ForEach(e => e.Remove());
        }

        public static void Schema(Settings settings)
        {
            SettingsSchema(settings);
            AnimeSchema(settings);
        }

        /// <summary>
        ///     Check the settings xml file for any inconsistencies in schema.
        /// </summary>
        public static void SettingsSchema(Settings settings)
        {
            Compare(settings.SettingsDocument.Root, Xml.Schema.SettingsDocument().Root);
            settings.SettingsDocument.Save(Settings.SettingsXml);
        }

        /// <summary>
        ///     Check the anime xml file for any inconsistencies in schema.
        /// </summary>
        public static void AnimeSchema(Settings settings)
        {
            var animes = settings.AnimeDocument.Root?.Elements();
            if (animes != null)
                foreach (var anime in animes)
                    Compare(anime, Xml.Schema.AnimeNode());
            settings.AnimeDocument.Save(Settings.AnimeXml);
        }
    }
}