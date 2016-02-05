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

        private string folderAnime;
        private string folderTorrent;
        private string programUtorrent;
        private string[] subgroups;

        public MainWindow() {
            InitializeComponent();
            loadSettings();
            updateTable();
            mainGrid.Visibility = Visibility.Visible;
        }

        private void button_folder_Click(object sender, RoutedEventArgs e) {
            Process.Start(folderAnime);
        }

        private void button_playlist_Click(object sender, RoutedEventArgs e) {
            string[] videos = Directory.GetDirectories(folderAnime)
                .Where(s => !s.EndsWith("torrents") && !s.EndsWith("Grace") && !s.EndsWith("Watched"))
                .SelectMany(f => Directory.GetFiles(f))
                .ToArray();
            using (StreamWriter file = new StreamWriter(path: folderAnime + @"\playlist.m3u", 
                                                        append: false)) {
                foreach(String video in videos)
                    file.WriteLine(video);
            }
        }

        private void button_settings_Click(object sender, RoutedEventArgs e) {
   
        }
        
        private void button_open_executing_Click(object sender, RoutedEventArgs e) {
            Process.Start(".");
        }

        private void addAnime() {
            XDocument anime = XDocument.Load("anime.xml");

            var element = new XElement("show",
                new XElement("name", "Fairy Tail Zero"),
                new XElement("episode", "04"),
                new XElement("status", "Watching"),
                new XElement("resolution", "720"),
                new XElement("airing", true),
                new XElement("updated", false),
                new XElement("name-strict", true),
                new XElement("last-downloaded", "2016-02-04"));

            anime.Element("anime").Add(element);
            anime.Save("anime.xml");
        }

        private void removeAnime(string name) {
            XDocument anime = XDocument.Load("anime.xml");
            var result = anime.Root.Elements().Where(x => x.Element("name").Value == name).FirstOrDefault();
            if (result != null) {
                result.Remove();
                anime.Save("anime.xml");
            }
        }

        private void createAnimeXML() {
            XDocument doc =
                    new XDocument(
                        new XDeclaration("1.0", "utf-8", "yes"),
                        new XComment("Anime mockup"),
                        new XElement("anime",
                            new XElement("show",
                                new XElement("name", "Fairy Tail Zero"),
                                new XElement("episode", "04"),
                                new XElement("status", "Watching"),
                                new XElement("resolution", "720"),
                                new XElement("airing", true),
                                new XElement("updated", false),
                                new XElement("name-strict", true),
                                new XElement("last-downloaded", "2016-02-04")))
                        );

            doc.Save("anime.xml");
        }

        private void createSettingsXML() {
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

        private void loadSettings() {
            if (!File.Exists("settings.xml"))
                createSettingsXML();
            
            XDocument settings = XDocument.Load("settings.xml");
            folderAnime = settings.Root.Element("path").Element("base").Value;
            folderTorrent = settings.Root.Element("path").Element("torrents").Value;
            programUtorrent = settings.Root.Element("path").Element("utorrent").Value;
            subgroups = settings.Root.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray();
        }

        private void updateTable() {
            var anime = XDocument.Load("anime.xml").Root;
            dataGrid.DataContext = anime;
        }

        private void button_test_add_Click(object sender, RoutedEventArgs e) {
            addAnime();
            updateTable();
        }

        private void button_test_delete_Click(object sender, RoutedEventArgs e) {
            removeAnime("Fairy Tail Zero");
            updateTable();
        }

        private void button_visibility_Click(object sender, RoutedEventArgs e) {
            dataGrid.Visibility = dataGrid.Visibility == Visibility.Hidden ?
                Visibility.Visible : Visibility.Hidden;
        }
    }
}

public class Anime {
    public string name { get; set; }
    public string episode { get; set; }
    public bool airing { get; set; }
    public string status { get; set; }
}
