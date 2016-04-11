using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Markup;
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
        ///     Compares the current schema to the default schema and adds any elements (up to depth level 2)
        /// </summary>
        /// <param name="currentSchema"></param>
        /// <param name="defaultSchema"></param>
        /// <param name="defaultValues">A dictionary of default values for keys</param>
        private static void Compare(XContainer currentSchema, XContainer defaultSchema, Dictionary<string, string> defaultValues)
        {
            
            if (currentSchema == null || defaultSchema == null)
                return;

            // add
            foreach (var element in defaultSchema.Elements())
            {
                if (!currentSchema?.Elements().Any(e => e.Name.Equals(element.Name)) == true)
                {
                    if (defaultValues.Keys.Any(key => key.Equals(element.Name.ToString())))
                        currentSchema?.Add(new XElement(element.Name), defaultValues[element.Name.ToString()]);
                    else
                        currentSchema?.Add(new XElement(element.Name));

                    if (element.HasElements)
                    {
                        // TODO: make this recursive
                        foreach (var childElement in element.Elements())
                        {
                            currentSchema?.Element(element.Name)?
                                .Add(defaultValues.Keys.Any(key => key.Equals(childElement.Name.ToString()))
                                    ? new XElement(childElement.Name, defaultValues[childElement.Name.ToString()])
                                    : new XElement(childElement.Name));
                        }
                    }
                }
            }

            // remove
            foreach (var element in currentSchema.Elements())
            {
                if (!defaultSchema.Elements().Any(e => e.Name.Equals(element.Name)))
                {
                    element.Remove();
                }
                // TODO: make another sub element maybe recursive check here too
            }
        }

        /// <summary>
        ///     Check the settings xml file for any inconsistencies in schema.
        /// </summary>
        public void SettingsSchema()
        {
            Compare(_controller.SettingsRoot, XmlCreate.SettingsXml().Root,
                new Dictionary<string, string> {{"sortBy", "name"}, {"use-logging", "false"}});
            _controller.SettingsDocument.Save(_settings.SettingsXmlPath);
        }

        /// <summary>
        ///     Check the anime xml file for any inconsistencies in schema.
        /// </summary>
        public void AnimeSchema()
        {
            foreach (var anime in _controller.AnimeRoot.Elements())
                Compare(anime, XmlCreate.AnimeNode(), null);
            _controller.AnimeDocument.Save(_settings.AnimeXmlPath);
        }
    }
}