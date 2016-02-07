using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using anime_downloader.Classes;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace anime_downloader {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private string applicationPath, settingsXMLPath, animeXMLPath;
        private string folder_base, folder_torrents, folder_utorrent;
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

            doc.Save(animeXMLPath);
        }

        private void addAnime(Anime newAnime) {
            XDocument anime = XDocument.Load(animeXMLPath);

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
            anime.Save(animeXMLPath);
        }

        private void editAnime(string name, Anime editedAnime) {
            XDocument doc = XDocument.Load(animeXMLPath);
            XElement anime = doc.Root;
            XElement selected = anime.Elements().Where(a => a.Element("name").Value.Equals(name)).FirstOrDefault();

            selected.Element("name").Value = editedAnime.name;
            selected.Element("episode").Value = editedAnime.episode;
            selected.Element("status").Value = editedAnime.status;
            selected.Element("resolution").Value = editedAnime.resolution;
            selected.Element("airing").Value = editedAnime.airing.ToString();
            selected.Element("name-strict").Value = editedAnime.nameStrict.ToString();

            doc.Save(animeXMLPath);
        }

        private void removeAnime(string name) {
            XDocument anime = XDocument.Load(animeXMLPath);
            var result = anime.Root.Elements().Where(x => x.Element("name").Value == name).FirstOrDefault();
            if (result != null) {
                result.Remove();
                anime.Save(animeXMLPath);
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

            doc.Save(settingsXMLPath);
        }

        private void loadSettings() {
            applicationPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "anime-downloader");

            if (!Directory.Exists(applicationPath))
                Directory.CreateDirectory(applicationPath);

            animeXMLPath = System.IO.Path.Combine(applicationPath, "anime.xml");
            settingsXMLPath = System.IO.Path.Combine(applicationPath, "settings.xml");

            if (!File.Exists(animeXMLPath))
                createAnimeXML();

            if (!File.Exists(settingsXMLPath))
                newSettings();
            
            else {
                XElement settings = XDocument.Load(settingsXMLPath).Root;
                folder_base = settings.Element("path").Element("base").Value;
                folder_torrents = settings.Element("path").Element("torrents").Value;
                folder_utorrent = settings.Element("path").Element("utorrent").Value;
                subgroups = settings.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray();
                onlyWhitelisted = Boolean.Parse(settings.Element("flag").Element("only-whitelisted-subs").Value);
            }
        }

        private void updateTable() {
            var anime = XDocument.Load(animeXMLPath).Root;
            var table = currentDisplay as UserControls.AnimeList;
            if (table != null)
                table.dataGrid.DataContext = anime;
            //dataGrid.DataContext = anime;
        }

        // 

        private async void downloadAnime(TextBox textbox, Anime[] animes) {
            toggleButtons(button_home, button_list, button_settings, button_check);
            int totalDownloaded = 0;
            textbox.Text = ">> Searching for currently airing anime episodes ...\n";

            foreach (Anime anime in animes) {
                var nyaaLink = await anime.getLinkToNextEpisode();

                if (nyaaLink != null) {

                    // Nyaa listing with no subgroup in the title
                    if (!nyaaLink.hasSubgroup()) {
                        if (onlyWhitelisted)
                            textbox.Text += $"Found result for {anime.name} with no subgroup. Skipping ...\n";
                    }

                    // Nyaa listing with subgroup
                    else if (!subgroups.Contains(nyaaLink.subgroup())) {
                        if (onlyWhitelisted) {
                            textbox.Text +=
                                $"Found result for {anime.name} with non-whitelisted subgroup. Skipping ...\n";
                        }
                    }

                    textbox.Text += $"Downloading '{anime.title()}' episode '{anime.nextEpisode()}'.\n";

                    string filepath = Path.Combine(folder_torrents, nyaaLink.torrentName());
                    if (!File.Exists(filepath))
                        new WebClient().DownloadFile(nyaaLink.link, filepath);

                    var command = $"/DIRECTORY \"{getOutputFolder()}\" \"{filepath}\"";
                    callCommand(folder_utorrent, command);

                    anime.episode = anime.nextEpisode();
                    // increment week last downloaded

                    editAnime(anime.name, anime);
                    updateTable();
                    totalDownloaded++;
                }
            }

            textbox.Text += totalDownloaded > 0 ? $">> Found {totalDownloaded} anime downloads." : ">> No new anime found.";
            toggleButtons(button_home, button_list, button_settings, button_check);
        }
        
        private void callCommand(string filename, string command) {
            Process proc = new Process();
            proc.StartInfo.FileName = filename;
            proc.StartInfo.Arguments = command;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            Task.Run(() => proc.Start());
            // return proc.StandardOutput.ReadToEnd();
        }

        private string getOutputFolder() {
            var date = DateTime.Now;
            var weeknumber = Math.Floor(Convert.ToDouble(date.DayOfYear) / 7);
            var folderName = $"{date.Year} - Week {weeknumber} - {date.ToString("MMMM")}";

            var outputPath = Path.Combine(folder_base, folderName);

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            return outputPath;
        }

        private void toggleButtons(params Button[] buttons) {
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

        //// Event handling
        // simple buttons

        private void button_folder_Click(object sender, RoutedEventArgs e) {
            if (Directory.Exists(folder_base))
                Process.Start(folder_base);
            else
                MessageBox.Show("Your base folder doesn't seem to exist.");
        }

        private void button_playlist_Click(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(folder_base))
                MessageBox.Show("Your base folder doesn't seem to exist.");
            else {
                string[] videos = Directory.GetDirectories(folder_base)
                    .Where(s => !s.EndsWith("torrents") && !s.EndsWith("Grace") && !s.EndsWith("Watched"))
                    .SelectMany(f => Directory.GetFiles(f))
                    .ToArray();
                using (StreamWriter file = new StreamWriter(path: folder_base + @"\playlist.m3u",
                    append: false)) {
                    foreach (String video in videos)
                        file.WriteLine(video);
                }
            }
        }

        private void button_open_executing_Click(object sender, RoutedEventArgs e) {
            Process.Start(applicationPath);
        }

        // anime list

        private void button_list_Click(object sender, RoutedEventArgs e) {
            display.Children.Clear();
            currentDisplay = new UserControls.AnimeList();
            display.Children.Add(currentDisplay);
            updateTable();

            var list = currentDisplay as UserControls.AnimeList;
            list.add.Click += new RoutedEventHandler(button_add_new_Click);
            list.edit.Click += new RoutedEventHandler(anime_list_edit_Click);
            list.delete.Click += new RoutedEventHandler(anime_list_delete_Click);
            list.dataGrid.PreviewKeyDown += new KeyEventHandler(anime_list_delete_KeyDown);
            list.dataGrid.MouseDoubleClick += new MouseButtonEventHandler(anime_list_MouseDoubleClick);
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

        private void anime_list_MouseDoubleClick(object sender, MouseEventArgs e) {
            UserControls.AnimeList list = currentDisplay as UserControls.AnimeList;
            if (list.dataGrid.SelectedCells.FirstOrDefault().IsValid) {
                anime_list_edit_Click(sender, e);
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
                    string[] subgroups = settings.subgroups_textbox.Text.Split(new string[] { ", " },
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

                    doc.Save(settingsXMLPath);
                    loadSettings();
                }
            }
        }

        private void newSettings() {
            display.Children.Clear();
            currentDisplay = new UserControls.Settings();
            display.Children.Add(currentDisplay);

            var settings = currentDisplay as UserControls.Settings;
            if (settings != null) {
                toggleButtons(button_home, button_list, button_settings, button_check, button_folder, button_playlist, button_open_executing);
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
                            settings.subgroups_textbox.Text.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries),
                            settings.only_whitelisted_checkbox.IsChecked.Value);
                        toggleButtons(button_home, button_list, button_settings, button_check, button_folder, button_playlist, button_open_executing);
                        loadSettings();
                        button_home.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                });

            }
        }

        // download
        
        private void button_check_Click(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(folder_base))
                MessageBox.Show("Your base folder doesn't seem to exist.");

            else {
                if (!Directory.Exists(folder_torrents))
                    Directory.CreateDirectory(folder_torrents);

                display.Children.Clear();
                currentDisplay = new UserControls.Download();
                display.Children.Add(currentDisplay);
                var download = currentDisplay as UserControls.Download;

                XDocument animeXML = XDocument.Load(animeXMLPath);
                Anime[] animes = animeXML.Element("anime").Elements()
                    .Select(x => new Anime(x))
                    .Where(a => a.airing == true)
                    .ToArray();
                string[] names = animes.Select(a => a.name).ToArray();

                if (download != null) {
                    downloadAnime(download.textBox, animes);
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

                KeyEventHandler enterApply = new KeyEventHandler((object s, KeyEventArgs k) => {
                    if (k.Key == Key.Enter) {
                        anime.add_button.Focus();
                        anime.add_button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                });

                anime.name_textbox.KeyUp += enterApply;
                anime.episode_textbox.KeyUp += enterApply;
            }
        }

        private void button_add_Click(object sender, RoutedEventArgs e) {
            var anime = currentDisplay as UserControls.Add;

            if (anime != null) {

                if (anime.name_textbox.Text.Equals("") || anime.episode_textbox.Text.Equals("")) {
                    MessageBox.Show("There needs to be a name and/or episode.");
                }

                else { 
                    Anime newAnime = new Anime {
                        name = anime.name_textbox.Text,
                        episode = string.Format("{0:D2}", int.Parse(anime.episode_textbox.Text)),
                        status = anime.status_combobox.Text,
                        resolution = anime.resolution_combobox.Text,
                        airing = anime.airing_checkbox.IsChecked.Value,
                        nameStrict = anime.name_strict_checkbox.IsChecked.Value
                    };
                    
                    addAnime(newAnime);
                    button_list.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            }
        }

        // edit anime

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

                    KeyEventHandler enterApply = new KeyEventHandler((object s, KeyEventArgs k) => {
                        if (k.Key == Key.Enter) {
                            anime.add_button.Focus();
                            anime.add_button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                    });

                    anime.name_textbox.KeyUp += enterApply;
                    anime.episode_textbox.KeyUp += enterApply;

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

        private void button_anime_edit_Click(object sender, RoutedEventArgs e) {
            var anime = currentDisplay as UserControls.Add;

            if (anime != null) {
                
                if (anime.name_textbox.Text.Equals("") || anime.episode_textbox.Text.Equals(""))
                    MessageBox.Show("There needs to be a name and/or episode.");

                else {
                    Anime editedAnime = new Anime {
                        name = anime.name_textbox.Text,
                        episode = string.Format("{0:D2}", int.Parse(anime.episode_textbox.Text)),
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
}
