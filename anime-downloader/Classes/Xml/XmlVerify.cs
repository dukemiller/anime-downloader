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
    public class XmlVerify
    {
        private readonly XmlController _controller;

        private readonly Settings _settings;

        public XmlVerify(Settings settings, XmlController controller)
        {
            _settings = settings;
            _controller = controller;
        }

        /// <summary>
        ///     Compares the current schema to the default schema and adds any elements
        /// </summary>
        private static void Compare(
            XContainer current,
            XContainer schema)
        {
            if (current == null || schema == null)
                return;

            var remove = new List<XElement>();

            // add
            foreach (var element in schema.Elements())
            {
                if (!current.Elements().Any(e => e.Name.Equals(element.Name)))
                    current.Add(new XElement(element.Name, element.Value));

                if (element.HasElements)
                    Compare(current.Element(element.Name.ToString()), element);
            }

            // remove
            foreach (var element in current.Elements())
            {
                if (!schema.Elements().Any(e => e.Name.Equals(element.Name)))
                    remove.Add(element);
            }

            remove.ForEach(e => e.Remove());
        }

        public void Schema()
        {
            SettingsSchema();
            AnimeSchema();
        }

        /// <summary>
        ///     Check the settings xml file for any inconsistencies in schema.
        /// </summary>
        public void SettingsSchema()
        {
            Compare(_controller.SettingsRoot, XmlSchema.SettingsXml().Root);
            _controller.SettingsDocument.Save(_settings.SettingsXml);
        }

        /// <summary>
        ///     Check the anime xml file for any inconsistencies in schema.
        /// </summary>
        public void AnimeSchema()
        {
            foreach (var anime in _controller.AnimeRoot.Elements())
                Compare(anime, XmlSchema.AnimeNode());
            _controller.AnimeDocument.Save(_settings.AnimeXml);
        }
    }
}