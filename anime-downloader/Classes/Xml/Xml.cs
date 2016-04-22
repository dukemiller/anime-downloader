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
        public XmlSchema Schema;

        /// <summary>
        ///     The XML file schema verifier class.
        /// </summary>
        public XmlVerify Verify;

        public Xml(Settings settings)
        {
            Controller = XmlController.GetXmlController(settings);
            Schema = new XmlSchema(settings);
            Verify = new XmlVerify(settings, Controller);
        }
    }
}