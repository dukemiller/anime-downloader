﻿using System;
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

        private string folder_base;
        private string folder_torrents;
        private string folder_utorrent;
        private bool onlyWhitelisted;
        private string[] subgroups;
        private UserControl currentDisplay;
        private string currentlyEditedAnime;

        public MainWindow() {
            InitializeComponent();
            currentDisplay = new UserControls.Home();
            display.Children.Add(currentDisplay);

            loadSettings();
            updateTable();
        }

        private void button_folder_Click(object sender, RoutedEventArgs e) {
            Process.Start(folder_base);
        }

        private void button_playlist_Click(object sender, RoutedEventArgs e) {
            string[] videos = Directory.GetDirectories(folder_base)
                .Where(s => !s.EndsWith("torrents") && !s.EndsWith("Grace") && !s.EndsWith("Watched"))
                .SelectMany(f => Directory.GetFiles(f))
                .ToArray();
            using (StreamWriter file = new StreamWriter(path: folder_base + @"\playlist.m3u", 
                                                        append: false)) {
                foreach(String video in videos)
                    file.WriteLine(video);
            }
        }

        private void button_open_executing_Click(object sender, RoutedEventArgs e) {
            Process.Start(".");
        }
        
        // Prototyping 
        
        private void createPremadeSettingsXML() {
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
       
        // XML Modification

        private void createAnimeXML() {
            XDocument doc =
                    new XDocument(
                        new XDeclaration("1.0", "utf-8", "yes"),
                        new XComment("Anime mockup"),
                        new XElement("anime"/*,
                            new XElement("show",
                                new XElement("name", "Fairy Tail Zero"),
                                new XElement("episode", "04"),
                                new XElement("status", "Watching"),
                                new XElement("resolution", "720"),
                                new XElement("airing", true),
                                new XElement("updated", false),
                                new XElement("name-strict", true),
                                new XElement("last-downloaded", "2016-02-04"))*/)
                        );

            doc.Save("anime.xml");
        }

        private void addAnime(Anime newAnime) {
            XDocument anime = XDocument.Load("anime.xml");

            var element = new XElement("show",
                new XElement("name", newAnime.name),
                new XElement("episode", newAnime.episode),
                new XElement("status", newAnime.status),
                new XElement("resolution", newAnime.resolution),
                new XElement("airing", newAnime.airing),
                new XElement("updated", false),
                new XElement("name-strict", newAnime.nameStrict),
                new XElement("last-downloaded", "2016-02-04"));

            anime.Element("anime").Add(element);
            anime.Save("anime.xml");
        }

        private void editAnime(string name, Anime editedAnime) {
            XDocument doc = XDocument.Load("anime.xml");
            XElement anime = doc.Root;
            XElement selected = anime.Elements().Where(a => a.Element("name").Value.Equals(name)).FirstOrDefault();

            selected.Element("name").Value = editedAnime.name;
            selected.Element("episode").Value = editedAnime.episode;
            selected.Element("status").Value = editedAnime.status;
            selected.Element("resolution").Value = editedAnime.resolution;
            selected.Element("airing").Value = editedAnime.airing.ToString();
            selected.Element("name-strict").Value = editedAnime.nameStrict.ToString();

            doc.Save("anime.xml");
        }

        private void removeAnime(string name) {
            XDocument anime = XDocument.Load("anime.xml");
            var result = anime.Root.Elements().Where(x => x.Element("name").Value == name).FirstOrDefault();
            if (result != null) {
                result.Remove();
                anime.Save("anime.xml");
            }
        }

        private void createSettingsXML(string basepath, string download, string utorrent, string[] subgroups, bool whitelisted) {

            XElement sub = new XElement("subgroup");
            foreach (String group in subgroups)
                sub.Add(new XElement("name", group));

            XDocument doc =
                    new XDocument(
                        new XDeclaration("1.0", "utf-8", "yes"),
                        new XComment("User profile settings"),
                        new XElement("settings",
                            new XElement("name", Environment.UserName),
                            new XElement("path",
                                new XElement("base", basepath),
                                new XElement("utorrent", utorrent),
                                new XElement("torrents", download)),
                            sub,
                            new XElement("flag",
                                new XElement("only-whitelisted-subs", whitelisted)))
                        );

            doc.Save("settings.xml");
        }

        private void loadSettings() {
            if (!File.Exists("anime.xml"))
                createAnimeXML();

            if (!File.Exists("settings.xml"))
                newSettings();
            
            else {
                XElement settings = XDocument.Load("settings.xml").Root;
                folder_base = settings.Element("path").Element("base").Value;
                folder_torrents = settings.Element("path").Element("torrents").Value;
                folder_utorrent = settings.Element("path").Element("utorrent").Value;
                subgroups = settings.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray();
                onlyWhitelisted = Boolean.Parse(settings.Element("flag").Element("only-whitelisted-subs").Value);
            }
        }

        // 

        private void toggle_buttons(params Button[] buttons) {
            foreach (Button b in buttons) {
                if (b.IsHitTestVisible) {
                    b.IsHitTestVisible = false;
                    b.Opacity = 0.4;
                }
                else {
                    b.IsHitTestVisible = true;
                    b.Opacity = 1.0;
                }
            }
        }

        private void updateTable() {
            var anime = XDocument.Load("anime.xml").Root;
            var table = currentDisplay as UserControls.AnimeList;
            if (table != null)
                table.dataGrid.DataContext = anime;
            //dataGrid.DataContext = anime;
        }

        private void newSettings() {
            display.Children.Clear();
            currentDisplay = new UserControls.Settings();
            display.Children.Add(currentDisplay);

            var settings = currentDisplay as UserControls.Settings;
            if (settings != null) {
                toggle_buttons(button_home, button_list, button_settings, button_check);
                settings.base_textbox.Text = Directory.GetCurrentDirectory();
                settings.torrent_textbox.Text = System.IO.Path.Combine(settings.base_textbox.Text, "torrents");
                settings.download_textbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
                settings.apply_changes_button.Content = "Create Profile";
                settings.apply_changes_button.Click += new RoutedEventHandler((object o, RoutedEventArgs e2) => {

                    if (settings.base_textbox.Text.Equals("") || settings.torrent_textbox.Text.Equals("") || settings.download_textbox.Text.Equals(""))
                        MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");
                    else {
                        createSettingsXML(settings.base_textbox.Text, 
                            settings.torrent_textbox.Text,
                            settings.download_textbox.Text, 
                            settings.subgroups_textbox.Text.Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries),
                            settings.only_whitelisted_checkbox.IsChecked.Value);
                        toggle_buttons(button_home, button_list, button_settings, button_check);
                        loadSettings();
                        button_home.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                });
                
            }
        }
        
        //// Event handling
        // anime list

        private void button_list_Click(object sender, RoutedEventArgs e) {
            display.Children.Clear();
            currentDisplay = new UserControls.AnimeList();
            display.Children.Add(currentDisplay);
            updateTable();

            var list = currentDisplay as UserControls.AnimeList;
            list.@new.Click += new RoutedEventHandler(button_add_new_Click);
            list.edit.Click += new RoutedEventHandler(anime_list_edit_Click);
            list.delete.Click += new RoutedEventHandler(anime_list_delete_Click);
            list.dataGrid.PreviewKeyDown += new KeyEventHandler(anime_list_delete_KeyDown);

        }

        private void anime_list_edit_Click(object sender, RoutedEventArgs e) {
            var table = currentDisplay as UserControls.AnimeList;

            if (table != null) {
                if (table.dataGrid.SelectedCells.FirstOrDefault().IsValid) {
                    XElement item = table.dataGrid.SelectedCells[0].Item as XElement; //  .Items[0].ToString());

                    display.Children.Clear();
                    currentDisplay = new UserControls.Add();
                    display.Children.Add(currentDisplay);

                    var anime = currentDisplay as UserControls.Add;
                    anime.add_button.Content = "Edit";
                    anime.add_button.Click += new RoutedEventHandler(button_anime_edit_Click);

                    anime.name_textbox.KeyUp += new KeyEventHandler((object s, KeyEventArgs k) => {
                        if (k.Key == Key.Enter) {
                            anime.add_button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                    });

                    anime.name_textbox.Text = item.Element("name").Value;
                    anime.episode_textbox.Text = item.Element("episode").Value;
                    anime.resolution_combobox.Text = item.Element("resolution").Value;
                    anime.status_combobox.Text = item.Element("status").Value;
                    anime.airing_checkbox.IsChecked = Boolean.Parse(item.Element("airing").Value);
                    anime.name_strict_checkbox.IsChecked = Boolean.Parse(item.Element("name-strict").Value);

                    currentlyEditedAnime = item.Element("name").Value;
                }
            }
        }
        
        private void anime_list_delete_Click(object sender, RoutedEventArgs e) {
            UserControls.AnimeList list = currentDisplay as UserControls.AnimeList;
            if (list.dataGrid.SelectedCells.FirstOrDefault().IsValid) {
                XElement row = list.dataGrid.SelectedCells.FirstOrDefault().Item as XElement;
                if (row != null) {
                    string name = row.Element("name").Value;
                    removeAnime(name);
                    updateTable();
                }
            }
        }

        private void anime_list_delete_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Delete) {
                var list = currentDisplay as UserControls.AnimeList;
                XElement row = list.dataGrid.SelectedCells[0].Item as XElement;
                string name = row.Element("name").Value;
                removeAnime(name);
                updateTable();
            }
        }

        // home

        private void button_home_Click(object sender, RoutedEventArgs e) {
            display.Children.Clear();
            currentDisplay = new UserControls.Home();
            display.Children.Add(currentDisplay);
        }
        
        // settings

        private void button_settings_Click(object sender, RoutedEventArgs e) {
            display.Children.Clear();
            currentDisplay = new UserControls.Settings();
            display.Children.Add(currentDisplay);

            var settings = currentDisplay as UserControls.Settings;
            if (settings != null) {
                settings.base_textbox.Text = folder_base;
                settings.subgroups_textbox.Text = String.Join(", ", subgroups);
                settings.download_textbox.Text = folder_utorrent;
                settings.torrent_textbox.Text = folder_torrents;
                settings.apply_changes_button.Click += new RoutedEventHandler(button_apply_settings_Click);
                settings.only_whitelisted_checkbox.IsChecked = onlyWhitelisted;
            }
        }

        private void button_apply_settings_Click(object sender, RoutedEventArgs e) {
            var settings = currentDisplay as UserControls.Settings;
            if (settings != null) {

                if (settings.base_textbox.Text.Equals("") || settings.torrent_textbox.Text.Equals("") ||
                    settings.download_textbox.Text.Equals(""))
                    MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

                else {
                    string[] subgroups = settings.subgroups_textbox.Text.Split(new string[] {", "},
                        StringSplitOptions.None);

                    var subgroup = new XElement("subgroup");
                    foreach (String sub in subgroups)
                        subgroup.Add(new XElement("name", sub));

                    XDocument doc =
                        new XDocument(
                            new XDeclaration("1.0", "utf-8", "yes"),
                            new XComment("User profile settings"),
                            new XElement("settings",
                                new XElement("name", "Duke"),
                                new XElement("path",
                                    new XElement("base", settings.base_textbox.Text),
                                    new XElement("utorrent", settings.download_textbox.Text),
                                    new XElement("torrents", settings.torrent_textbox.Text)),
                                subgroup,
                                new XElement("flag",
                                    new XElement("only-whitelisted-subs", settings.only_whitelisted_checkbox.IsChecked)))
                            );

                    doc.Save("settings.xml");
                    loadSettings();
                }
            }
        }

        // new anime

        private void button_add_new_Click(object sender, RoutedEventArgs e) {
            display.Children.Clear();
            currentDisplay = new UserControls.Add();
            display.Children.Add(currentDisplay);
            var anime = currentDisplay as UserControls.Add;
            if (anime != null) {
                anime.add_button.Click += new RoutedEventHandler(button_add_Click);
            }
        }

        private void button_add_Click(object sender, RoutedEventArgs e) {
            var anime = currentDisplay as UserControls.Add;
            if (anime != null) {
                Anime newAnime = new Anime {
                    name = anime.name_textbox.Text,
                    episode = anime.episode_textbox.Text,
                    status = anime.status_combobox.Text,
                    resolution = anime.resolution_combobox.Text,
                    airing = anime.airing_checkbox.IsChecked.Value,
                    nameStrict = anime.name_strict_checkbox.IsChecked.Value
                };

                if (!newAnime.name.Equals("")) {
                    addAnime(newAnime);
                    // button_add_new.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    button_list.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            }
        }

        // edit anime

        private void button_anime_edit_Click(object sender, RoutedEventArgs e) {
            var anime = currentDisplay as UserControls.Add;

            if (anime != null) {
                Anime editedAnime = new Anime {
                    name = anime.name_textbox.Text,
                    episode = anime.episode_textbox.Text,
                    status = anime.status_combobox.Text,
                    resolution = anime.resolution_combobox.Text,
                    airing = anime.airing_checkbox.IsChecked.Value,
                    nameStrict = anime.name_strict_checkbox.IsChecked.Value
                };
                
                editAnime(currentlyEditedAnime, editedAnime);
                button_list.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }
        
    }
}

public class Anime {
    public string name { get; set; }
    public string episode { get; set; }
    public string status { get; set; }
    public string resolution { get; set; }
    public bool airing { get; set; }
    public bool nameStrict { get; set; }
}