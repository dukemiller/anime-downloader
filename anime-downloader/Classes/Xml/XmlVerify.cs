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
        ///     Check the settings xml file for any inconsistencies in schema.
        /// </summary>
        public void SettingsSchema()
        {
            // var document = XDocument.Load(_settings.SettingsXmlPath);
            // var root = document.Root;

            var root = _controller.SettingsRoot;

            if (root?.Element("sortBy") == null)
                root?.Add(new XElement("sortBy", "name"));

            if (root?.Element("flag")?.Element("use-logging") == null)
                root?.Element("flag")?.Add(new XElement("use-logging", false));

            _controller.SettingsDocument.Save(_settings.SettingsXmlPath);
        }

        /// <summary>
        ///     Check the anime xml file for any inconsistencies in schema.
        /// </summary>
        public void AnimeSchema()
        {
            var root = _controller.AnimeRoot;

            if (root != null)
            {
                foreach (var anime in root.Elements().Where(anime => anime.Element("preferredSubgroup") == null))
                {
                    anime.Add(new XElement("preferredSubgroup", ""));
                }
            }

            _controller.AnimeDocument.Save(_settings.AnimeXmlPath);
        }
    }
}