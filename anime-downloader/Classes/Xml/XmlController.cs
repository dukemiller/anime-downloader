using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace anime_downloader.Classes.Xml
{
    /// <summary>
    ///     The purpose of this class is to consolidate operations that may require
    ///     both read/write operations on the XML files to one location.
    /// </summary>
    public class XmlController
    {
        private static XmlController _xmlController;

        private readonly Settings _settings;

        private XDocument _animeDocument;

        private XDocument _settingsDocument;

        /// <summary>
        ///     Singleton-style private access constructor.
        /// </summary>
        /// <param name="settings"></param>
        private XmlController(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        ///     The document object for the anime xml.
        /// </summary>
        public XDocument AnimeDocument => _animeDocument ?? (_animeDocument = XDocument.Load(_settings.AnimeXmlPath));

        /// <summary>
        ///     The document object for the settings xml.
        /// </summary>
        public XDocument SettingsDocument
            => _settingsDocument ?? (_settingsDocument = XDocument.Load(_settings.SettingsXmlPath));

        /// <summary>
        ///     Retrieve a static collection of the anime currently in the anime xml as Anime objects.
        /// </summary>
        public IEnumerable<Anime> Animes => from anime in AnimeRoot.Elements()
            select new Anime(anime, this);

        /// <summary>
        ///     Retrieve a static and settings-defined sorted collection of the anime currently in the
        ///     anime xml as Anime objects.
        /// </summary>
        public IEnumerable<Anime> SortedAnimes => Animes.SortedWith(_settings.SortBy);

        /// <summary>
        ///     Retrieve a static collection of the anime that is both airing and being watched from the
        ///     anime xml as Anime objects.
        /// </summary>
        public IEnumerable<Anime> AiringAnimes => Animes.Where(a => a.Airing && a.Status == "Watching");

        /// <summary>
        ///     A flag that will save any change in the xml schema as soon as it happens.
        /// </summary>
        public bool AutoSave { get; set; } = true;

        /// <summary>
        ///     The root for manipulating the settings document.
        /// </summary>
        /// <returns></returns>
        public XElement SettingsRoot => SettingsDocument?.Root;

        /// <summary>
        ///     The root for manipulating the anime document.
        /// </summary>
        /// <returns></returns>
        public XElement AnimeRoot => AnimeDocument.Root;

        /// <summary>
        ///     Singleton static constructor for retrieving the same instance on any class.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static XmlController GetXmlController(Settings settings)
        {
            return _xmlController ?? (_xmlController = new XmlController(settings));
        }

        /// <summary>
        ///     Singleton instance grabber with no _settings, only use this in the case where
        ///     you know you have already instantiated XMLController atleast once
        /// </summary>
        /// <remarks>
        ///     I guess this is almost sort of just making _settings a potential global variable
        /// </remarks>
        /// <returns></returns>
        public static XmlController GetXmlController()
        {
            if (_xmlController != null)
                return _xmlController;
            throw new XmlNotInstantiatedWithSettings();
        }

        /// <summary>
        ///     Add an anime instance to the current anime xml.
        /// </summary>
        /// <param name="anime"></param>
        public void Add(Anime anime)
        {
            AnimeRoot.Add(anime.Root);
            if (AutoSave)
                SaveAnime();
        }

        /// <summary>
        ///     Remove an already existing anime from the current anime xml.
        /// </summary>
        /// <param name="anime"></param>
        public void Remove(Anime anime)
        {
            AnimeRoot.Elements().FirstOrDefault(a => a.Element("name")?.Value.Equals(anime.Name) ?? false)?.Remove();
            if (AutoSave)
                SaveAnime();
        }

        /// <summary>
        ///     Explicitly save the anime xml.
        /// </summary>
        public void SaveAnime()
        {
            AnimeDocument.Save(_settings.AnimeXmlPath);
        }

        /// <summary>
        ///     Explicitly save the settings xml.
        /// </summary>
        public void SaveSettings()
        {
            SettingsDocument.Save(_settings.SettingsXmlPath);
        }

        public class XmlNotInstantiatedWithSettings : Exception
        {}
    }
}