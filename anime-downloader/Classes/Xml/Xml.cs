namespace anime_downloader.Classes.Xml {
    public class Xml {
        public XmlController Controller;
        public XmlCreate Create;
        public XmlVerify Verify;

        public Xml(Settings settings) {
            Controller = XmlController.GetXmlController(settings);
            Create = new XmlCreate(settings);
            Verify = new XmlVerify(settings);
        }
    }
}
