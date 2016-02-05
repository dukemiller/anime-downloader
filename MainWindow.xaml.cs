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

namespace anime_downloader {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        String animeFolder;

        public MainWindow() {
            InitializeComponent();

            animeFolder = @"D:\Output\anime downloader";
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
    }
}
