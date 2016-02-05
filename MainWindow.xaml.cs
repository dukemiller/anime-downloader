using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml;


namespace anime_downloader {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        String animeFolder;

        public MainWindow() {
            InitializeComponent();
            loadSettings();
        }

        private void button_folder_Click(object sender, RoutedEventArgs e) {
            Process.Start(animeFolder);
        }

        private void button_playlist_Click(object sender, RoutedEventArgs e) {
            string[] videos = Directory.GetDirectories(animeFolder)
                .Where(s => !s.EndsWith("torrents") && !s.EndsWith("Grace") && !s.EndsWith("Watched"))
                .SelectMany(f => Directory.GetFiles(f))
                .ToArray();
            using (StreamWriter file = new StreamWriter(path: animeFolder + @"\playlist.m3u", 
                                                        append: false)) {
                foreach(String video in videos)
                    file.WriteLine(video);
            }
        }

        private void button_settings_Click(object sender, RoutedEventArgs e) {
            
            XDocument doc = 
                new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("User profile settings"),
                    new XElement("settings",
                        new XElement("name", "Duke"),
                        new XElement("path",
                            new XElement("base", @"D:\Output\anime downloader"),
                            new XElement("utorrent", @"C:\Program Files (x86)\uTorrent\uTorrent.exe"),
                            new XElement("torrents", @"D:\Output\anime downloader\torrents")),
                        new XElement("subgroup",
                            new XElement("name", "BakedFish"),
                            new XElement("name", "HorribleSubs"),
                            new XElement("name", "DeadFish"),
                            new XElement("name", "Pyon"),
                            new XElement("name", "kdfss")),
                        new XElement("flag",
                            new XElement("only-whitelisted-subs", "true")))
                );

            doc.Save("settings.xml");
        }

        private void button_Click(object sender, RoutedEventArgs e) {
            //var path = settings.Root.Elements().Select(x => x.Element("base-path")).First().ToString();
            //MessageBox.Show(path);
            // XmlTextReader reader = new XmlTextReader("settings.xml");
            //reader.Read()
        }

        private void loadSettings() {
            var settings = XDocument.Load("settings.xml");
            animeFolder = settings.Root.Element("path").Element("base").Value;
        }
    }
}
