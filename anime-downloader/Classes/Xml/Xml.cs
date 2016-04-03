namespace anime_downloader.Classes.Xml
{
    public class Xml
    {
        /// <summary>
        ///     The XML file data controlling class.
        /// </summary>
        public XmlController Controller;

        /// <summary>
        ///     The XML file creator class.
        /// </summary>
        public XmlCreate Create;

        /// <summary>
        ///     The XML file schema verifier class.
        /// </summary>
        public XmlVerify Verify;

        public Xml(Settings settings)
        {
            Controller = XmlController.GetXmlController(settings);
            Create = new XmlCreate(settings);
            Verify = new XmlVerify(settings, Controller);
        }
    }
}