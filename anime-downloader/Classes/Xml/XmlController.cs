using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace anime_downloader.Classes.Xml {
    public class XmlController {

        private static XmlController _xmlController;

        private readonly Settings _settings;

        private XDocument _animeDocument;

        public XDocument AnimeDocument => _animeDocument ?? (_animeDocument = XDocument.Load(_settings.AnimeXmlPath));

        private XDocument _settingsDocument;

        public XDocument SettingsDocument => _settingsDocument ?? (_settingsDocument = XDocument.Load(_settings.SettingsXmlPath));

        public IEnumerable<Anime> Animes => 
            from anime in AnimeDocument.Root?.Elements()
            select new Anime(anime, this);

        public bool AutoSave { get; set; } = true;

        public static XmlController GetXmlController(Settings settings) {
            return _xmlController ?? (_xmlController = new XmlController(settings));
        }

        private XmlController(Settings settings) {
            _settings = settings;
        }

        public XElement SettingsRoot() {
            return SettingsDocument?.Root;
        } 
        
        public void Add(Anime anime) {
            AnimeDocument.Root?.Add(anime.Root);
            if (AutoSave)
                SaveAnime();
        }

        public void Remove(Anime anime) {
            AnimeDocument.Root?.Elements().FirstOrDefault(a => a.Element("name")?.Value.Equals(anime.Name) ?? false)?.Remove();
            if (AutoSave)
                SaveAnime();
        }

        public void SaveAnime() {
            AnimeDocument.Save(_settings.AnimeXmlPath);
        }

        public void SaveSettings() {
            SettingsDocument.Save(_settings.SettingsXmlPath);
        }
    }
}