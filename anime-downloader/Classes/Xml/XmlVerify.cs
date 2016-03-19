using System.Linq;
using System.Xml.Linq;

namespace anime_downloader.Classes.Xml {
    public class XmlVerify {
        private readonly Settings _settings;

        public XmlVerify(Settings settings) {
            _settings = settings;
        }

        /// <summary>
        ///     Kind of a lazy way to test XML changes between versions.
        /// </summary>
        public void SettingsSchema() {
            var document = XDocument.Load(_settings.SettingsXmlPath);
            var root = document.Root;

            if (root?.Element("sortBy") == null)
                root?.Add(new XElement("sortBy", "name"));

            document.Save(_settings.SettingsXmlPath);
        }

        /// <summary>
        ///     Kind of a lazy way to test XML changes between versions.
        /// </summary>
        public void AnimeSchema() {
            var document = XDocument.Load(_settings.AnimeXmlPath);
            var root = document.Root;

            if (root != null) {
                foreach (var anime in root.Elements().Where(anime => anime.Element("preferredSubgroup") == null)) {
                    anime.Add(new XElement("preferredSubgroup", ""));
                }
            }

            document.Save(_settings.AnimeXmlPath);
        }
        
    }
}