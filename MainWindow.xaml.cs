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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace anime_downloader {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        /// <summary>
        /// The path and user settings object.
        /// </summary>
        private Settings settings;

        /// <summary>
        /// The current display on the right window pane.
        /// </summary>
        private UserControl currentDisplay;

        /// <summary>
        /// A helper for modifying anime.
        /// </summary>
        private string currentlyEditedAnime;

        public MainWindow() {
            InitializeComponent();
            changeDisplay(new UserControls.Home());
            initializeSettings();
            updateTable();
        }
        
        // XML Modification

        /// <summary>
        /// Create the Anime XML file with initial nodes.
        /// </summary>
        private void createAnimeXML() {
            XDocument document =
                    new XDocument(
                        new XDeclaration("1.0", "utf-8", "yes"),
                        new XComment("The anime list."),
                        new XElement("anime")
                        );
            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        /// Add a single anime into the anime XML file.
        /// </summary>
        /// <param name="anime">A valid anime object.</param>
        private void addAnime(Anime anime) {
            XDocument document = XDocument.Load(settings.animeXMLPath);

            XElement element = new XElement("show",
                new XElement("name", anime.name),
                new XElement("episode", anime.episode),
                new XElement("status", anime.status),
                new XElement("resolution", anime.resolution),
                new XElement("airing", anime.airing),
                new XElement("updated", false),
                new XElement("name-strict", anime.nameStrict),
                new XElement("last-downloaded", "2016-02-04"));

            document.Element("anime").Add(element);
            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        /// Add multiple animes into the anime XML file.
        /// </summary>
        /// <param name="animes">A collection of valid animes.</param>
        private void addAnime(List<Anime> animes) {
            XDocument document = XDocument.Load(settings.animeXMLPath);

            foreach (Anime anime in animes) {
                XElement element = new XElement("show",
                    new XElement("name", anime.name),
                    new XElement("episode", anime.episode),
                    new XElement("status", anime.status),
                    new XElement("resolution", anime.resolution),
                    new XElement("airing", anime.airing),
                    new XElement("updated", false),
                    new XElement("name-strict", anime.nameStrict),
                    new XElement("last-downloaded", "2016-02-04"));
                document.Element("anime").Add(element);
            }

            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        /// Edit a single anime and write that change to the anime XML file.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        /// <param name="anime">The replacing anime object.</param>
        private void editAnime(string name, Anime anime) {
            XDocument document = XDocument.Load(settings.animeXMLPath);
            XElement root = document.Root;

            XElement selected = root.Elements()
                .Where(a => a.Element("name").Value.Equals(name))
                .FirstOrDefault();

            if (selected != null) {
                selected.Element("name").Value = anime.name;
                selected.Element("episode").Value = anime.episode;
                selected.Element("status").Value = anime.status;
                selected.Element("resolution").Value = anime.resolution;
                selected.Element("airing").Value = anime.airing.ToString();
                selected.Element("name-strict").Value = anime.nameStrict.ToString();
                document.Save(settings.animeXMLPath);
            }
        }

        /// <summary>
        /// Edit a specific elementName about an anime instead of needing an entire object.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        /// <param name="elementName">The identifying element name.</param>
        /// <param name="elementValue">The value to be written to the element.</param>
        private void editAnime(string name, string elementName, string elementValue) {
            XDocument document = XDocument.Load(settings.animeXMLPath);
            XElement root = document.Root;

            XElement selected = root.Elements()
                .Where(a => a.Element("name").Value.Equals(name))
                .FirstOrDefault();

            if (selected != null) {
                var element = selected.Element(elementName);
                if (element != null) {
                    element.Value = elementValue;
                    document.Save(settings.animeXMLPath);
                }
            }

        }

        /// <summary>
        /// Edit multiple anime and write those changes to the anime XML file.
        /// </summary>
        /// <remarks>This works under the assumption that all Anime "name" are not modified.</remarks>
        /// <param name="animes">A collection of valid anime objects.</param>
        private void editAnime(List<Anime> animes) {
            XDocument document = XDocument.Load(settings.animeXMLPath);
            XElement root = document.Root;

            foreach (Anime anime in animes) {

                XElement selected = root.Elements()
                    .Where(a => a.Element("name").Value.Equals(anime.name))
                    .FirstOrDefault();

                if (selected != null) {
                    selected.Element("name").Value = anime.name;
                    selected.Element("episode").Value = anime.episode;
                    selected.Element("status").Value = anime.status;
                    selected.Element("resolution").Value = anime.resolution;
                    selected.Element("airing").Value = anime.airing.ToString();
                    selected.Element("name-strict").Value = anime.nameStrict.ToString();
                }
            }

            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        /// Remove a single anime from the anime XML file.
        /// </summary>
        /// <param name="name">The identifying key name.</param>
        private void removeAnime(string name) {
            XDocument document = XDocument.Load(settings.animeXMLPath);
            XElement node = document.Root.Elements().Where(x => x.Element("name").Value == name).FirstOrDefault();
            if (node != null) {
                node.Remove();
                document.Save(settings.animeXMLPath);
            }
        }

        /// <summary>
        /// Create a new XML file and populate it with the values from a settings object.
        /// </summary>
        /// <param name="settings">A valid instantiated settings object.</param>
        private void createSettingsXML(Settings settings) {

            XElement subgroups = new XElement("subgroup");
            foreach (String group in settings.subgroups)
                subgroups.Add(new XElement("name", group));

            XDocument document =
                    new XDocument(
                        new XDeclaration("1.0", "utf-8", "yes"),
                        new XComment("User profile settings"),
                        new XElement("settings",
                            new XElement("name", Environment.UserName),
                            new XElement("path",
                                new XElement("base", settings.baseFolderPath),
                                new XElement("torrents", settings.torrentFilesPath),
                                new XElement("utorrent", settings.utorrentPath)),
                            subgroups,
                            new XElement("flag",
                                new XElement("only-whitelisted-subs", settings.onlyWhitelisted)))
                        );

            document.Save(settings.settingsXMLPath);
        }

        // implement
        private void editSettingsXML(Settings settings) {
            
        }

        /// <summary>
        /// Initialize and set the settings object.
        /// </summary>
        private void initializeSettings() {

            string applicationPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "anime-downloader");

            if (!Directory.Exists(applicationPath))
                Directory.CreateDirectory(applicationPath);

            string settingsXMLPath = Path.Combine(applicationPath, "settings.xml");

            if (!File.Exists(settingsXMLPath))
                newSettings();

            XElement xmlSettings = XDocument.Load(settingsXMLPath).Root;

            settings = new Settings {
                applicationPath = applicationPath,
                animeXMLPath = Path.Combine(applicationPath, "anime.xml"),
                settingsXMLPath = settingsXMLPath,
                baseFolderPath = xmlSettings.Element("path").Element("base").Value,
                torrentFilesPath = xmlSettings.Element("path").Element("torrents").Value,
                utorrentPath = xmlSettings.Element("path").Element("utorrent").Value,
                subgroups = xmlSettings.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray(),
                onlyWhitelisted = Boolean.Parse(xmlSettings.Element("flag").Element("only-whitelisted-subs").Value)
            };
            
            if (!File.Exists(settings.animeXMLPath))
                createAnimeXML();
            
        }

        /// <summary>
        /// Update the values in the AnimeList table.
        /// </summary>
        private void updateTable() {
            XElement root = XDocument.Load(settings.animeXMLPath).Root;
            var animeListDisplay = currentDisplay as UserControls.AnimeList;
            if (animeListDisplay != null)
                animeListDisplay.dataGrid.DataContext = root;
        }

        // Helper functions

        /// <summary>
        /// Attempt to download anime and display the results.
        /// </summary>
        /// <param name="textbox">The output box to display results to.</param>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        private async void downloadAnime(TextBox textbox, Anime[] animes) {
            List<Anime> changedAnime = new List<Anime>();
            WebClient client = new WebClient();
            int totalDownloaded = 0;
            
            toggleButtons(button_home, button_list, button_settings, button_check);
            textbox.Text = ">> Searching for currently airing anime episodes ...\n";

            foreach (Anime anime in animes) {
                var nyaaLink = await anime.getLinkToNextEpisode();

                if (nyaaLink != null) {

                    // Nyaa listing with no subgroup in the title
                    if (!nyaaLink.hasSubgroup()) {
                        if (settings.onlyWhitelisted) {
                            textbox.AppendText($"Found result for {anime.name} with no subgroup. Skipping ...\n");
                            scrolldownTextbox(textbox);
                            continue;
                        }
                    }

                    // Nyaa listing with subgroup
                    else if (!settings.subgroups.Contains(nyaaLink.subgroup())) {
                        if (settings.onlyWhitelisted) {
                            textbox.AppendText($"Found result for {anime.name} with non-whitelisted subgroup. Skipping ...\n");
                            scrolldownTextbox(textbox);
                            continue;
                        }
                    }
                    
                    textbox.AppendText($"Downloading '{anime.title()}' episode '{anime.nextEpisode()}'.\n");
                    scrolldownTextbox(textbox);
                    string filepath = Path.Combine(settings.torrentFilesPath, nyaaLink.torrentName());

                    if (!File.Exists(filepath))
                        await client.DownloadFileTaskAsync(nyaaLink.link, filepath);

                    var command = $"/DIRECTORY \"{getOutputFolder()}\" \"{filepath}\"";
                    callCommand(settings.utorrentPath, command);
                    anime.episode = anime.nextEpisode();
                    changedAnime.Add(anime);
                    totalDownloaded++;
                }
            }

            editAnime(changedAnime);
            updateTable();
            textbox.AppendText(totalDownloaded > 0 ? $">> Found {totalDownloaded} anime downloads." : ">> No new anime found.");
            scrolldownTextbox(textbox);
            toggleButtons(button_home, button_list, button_settings, button_check);
        }
        
        /// <summary>
        /// Check if Nyaa.eu is online within 1.0 seconds so not to hang when entering download view. 
        /// </summary>
        /// <returns></returns>
        private async Task<bool> NyaaIsOnline() {
            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create("http://www.nyaa.eu/");
            httpReq.Timeout = 1000;
            httpReq.AllowAutoRedirect = false;
            try {
                HttpWebResponse httpRes = await Task.Run(() => (HttpWebResponse)httpReq.GetResponse());
                return httpRes.StatusCode == HttpStatusCode.OK;
            }
            catch {
                return false;
            }

        }

        /// <summary>
        /// Execute new process with given parameters.
        /// </summary>
        /// <param name="executable">Path to the executable file.</param>
        /// <param name="parameters">Arguments given to the executable.</param>
        private void callCommand(string executable, string parameters) {
            Process proc = new Process();
            proc.StartInfo.FileName = executable;
            proc.StartInfo.Arguments = parameters;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            Task.Run(() => proc.Start());
        }

        /// <summary>
        /// Create and return the path to a folder based on a timestamp of the current moment.
        /// </summary>
        /// <returns>A path used to download into.</returns>
        private string getOutputFolder() {
            var date = DateTime.Now;
            var weeknumber = Math.Floor(Convert.ToDouble(date.DayOfYear) / 7);
            var folderName = $"{date.Year} - Week {weeknumber} - {date.ToString("MMMM")}";

            var outputPath = Path.Combine(settings.baseFolderPath, folderName);

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            return outputPath;
        }

        /// <summary>
        /// Toggle opacity and visibility of arbitrary amount of buttons.
        /// </summary>
        /// <param name="buttons">Any button element.</param>
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

        /// <summary>
        /// Simulate a button press.
        /// </summary>
        /// <param name="button">The button to press.</param>
        private void pressButton(Button button) {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        /// <summary>
        /// Change the current main display to another display.
        /// </summary>
        /// <param name="userDisplay">The user supplied display.</param>
        private void changeDisplay(UserControl userDisplay) {
            display.Children.Clear();
            currentDisplay = userDisplay;
            display.Children.Add(currentDisplay);
        }

        /// <summary>
        /// Strip the video name of all tags (resolution, seeders, etc).
        /// </summary>
        /// <param name="name">A downloaded file's name.</param>
        /// <returns></returns>
        private string stripFilename(string name) {
            string pattern = @"(\[(?:.*?)\])|(\((?:.*)\))";
            string scrubbedName = name;
            foreach (Match match in Regex.Matches(name, pattern)) {
                if (match.Groups.Count > 1) {
                    var m = match.Groups[1]?.Value ?? match.Groups[2]?.Value ?? "";
                    if (m.Length > 0)
                        scrubbedName = scrubbedName.Replace(m, "").Trim();
                }
            }
            return scrubbedName;
        }

        /// <summary>
        /// Returns a collection of [{animeName: lastGivenEpisode}] from a list of stripped titles.
        /// </summary>
        /// <param name="strippedNames">A collection of names passed through stripFilename().</param>
        /// <returns></returns>
        private Dictionary<string, int> collectLastEpisode(string[] strippedNames) {
            Dictionary<string, int> latest = new Dictionary<string, int>();

            foreach (string name in strippedNames) {
                var animeName = string.Join(" - ",
                    name.Split(new string[] { " .mp4", " .mkv" }, StringSplitOptions.RemoveEmptyEntries)[0]
                        .Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries)
                        .TakeWhile(s => !s.All(c => Char.IsNumber(c))));
                var animeEpisode = int.Parse(name.Split('-').Last().Split()[1]);

                if (!latest.ContainsKey(animeName))
                    latest.Add(animeName, animeEpisode);
                else
                    if (latest[animeName] < animeEpisode)
                        latest[animeName] = animeEpisode;
            }

            return latest;
        }

        /// <summary>
        /// Set all anime episode counts in the anime list to their last known values from the "finished" folder.
        /// </summary>
        /// <remarks>This is for re-indexing if you don't know which episodes you watched last.</remarks>
        private void setAnimeEpisodeTotalToLastKnown() {

            string path = Path.Combine(settings.baseFolderPath, "watched");

            var finishedAnimes = collectLastEpisode(Directory.GetFiles(path)
                .Select(f => Path.GetFileName(f))
                .Select(n => stripFilename(n))
                .ToArray());
            

            // I'm not sure how to cleanly turn this into a generic XML function above.
            XDocument document = XDocument.Load(settings.animeXMLPath);
            XElement root = document.Root;

            foreach (KeyValuePair<string, int> entry in finishedAnimes) {
                XElement selected = root.Elements()
                                        .Where(a => entry.Key.ToLower().Contains(a.Element("name").Value.ToLower()))
                                        .FirstOrDefault();
                if (selected != null)
                    selected.Element("episode").Value = string.Format("{0:D2}", entry.Value);
            }

            document.Save(settings.animeXMLPath);
        }
        
        /// <summary>
        /// Scroll to the bottom of a textbox.
        /// </summary>
        /// <param name="textbox">The textbox that will be scrolled down in.</param>
        private void scrolldownTextbox(TextBox textbox) {
            textbox.Focus();
            textbox.CaretIndex = textbox.Text.Length;
            textbox.ScrollToEnd();
        }

        // Event handling

        /// <summary>
        /// The home view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_home_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new UserControls.Home());
        }

        private void button_folder_Click(object sender, RoutedEventArgs e) {
            if (Directory.Exists(settings.baseFolderPath))
                Process.Start(settings.baseFolderPath);
            else
                MessageBox.Show("Your base folder doesn't seem to exist.");
        }

        private void button_playlist_Click(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(settings.baseFolderPath))
                MessageBox.Show("Your base folder doesn't seem to exist.");
            else {
                string[] videos = Directory.GetDirectories(settings.baseFolderPath)
                    .Where(s => !s.EndsWith("torrents") && !s.EndsWith("Grace") && !s.EndsWith("Watched"))
                    .SelectMany(f => Directory.GetFiles(f))
                    .ToArray();
                using (StreamWriter file = new StreamWriter(path: Path.Combine(settings.baseFolderPath, "playlist.m3u"), append: false)) {
                    foreach (String video in videos)
                        file.WriteLine(video);
                }
            }
        }

        private void button_open_executing_Click(object sender, RoutedEventArgs e) {
            Process.Start(settings.applicationPath);
        }

        /// <summary>
        /// The view to display the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_list_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new UserControls.AnimeList());
            updateTable();

            var animeListDisplay = currentDisplay as UserControls.AnimeList;
            animeListDisplay.add.Click += new RoutedEventHandler(button_add_new_Click);
            animeListDisplay.edit.Click += new RoutedEventHandler(anime_list_edit_Click);
            animeListDisplay.delete.Click += new RoutedEventHandler(anime_list_delete_Click);
            animeListDisplay.dataGrid.PreviewKeyDown += new KeyEventHandler(anime_list_delete_KeyDown);
            animeListDisplay.dataGrid.MouseDoubleClick += new MouseButtonEventHandler(anime_list_MouseDoubleClick);
        }

        /// <summary>
        /// The context menu event for the anime list view, selection: "Delete"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_delete_Click(object sender, RoutedEventArgs e) {
            var animeListDisplay = currentDisplay as UserControls.AnimeList;
            if (animeListDisplay.dataGrid.SelectedCells.FirstOrDefault().IsValid) {
                XElement row = animeListDisplay.dataGrid.SelectedCells.FirstOrDefault().Item as XElement;
                if (row != null) {
                    removeAnime(row.Element("name").Value);
                    updateTable();
                }
            }
        }

        /// <summary>
        /// The keydown event for the anime list view. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_delete_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Delete) {
                var animeListDisplay = currentDisplay as UserControls.AnimeList;
                XElement row = animeListDisplay.dataGrid.SelectedCells[0].Item as XElement;
                removeAnime(row.Element("name").Value);
                updateTable();
            }
        }

        /// <summary>
        /// The double click event for the anime list view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_MouseDoubleClick(object sender, MouseEventArgs e) {
            var animeListDisplay = currentDisplay as UserControls.AnimeList;
            if (animeListDisplay.dataGrid.SelectedCells.FirstOrDefault().IsValid) {
                anime_list_edit_Click(sender, e);
            }
        }

        /// <summary>
        /// The view to manage settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_settings_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new UserControls.Settings());

            var settingsDisplay = currentDisplay as UserControls.Settings;
            if (settings != null) {
                settingsDisplay.base_textbox.Text = settings.baseFolderPath;
                settingsDisplay.subgroups_textbox.Text = String.Join(", ", settings.subgroups);
                settingsDisplay.download_textbox.Text = settings.utorrentPath;
                settingsDisplay.torrent_textbox.Text = settings.torrentFilesPath;
                settingsDisplay.apply_changes_button.Click += new RoutedEventHandler(button_apply_settings_Click);
                settingsDisplay.only_whitelisted_checkbox.IsChecked = settings.onlyWhitelisted;
            }
        }

        /// <summary>
        /// The submission button event for the edit settings view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_apply_settings_Click(object sender, RoutedEventArgs e) {
            var settingsDisplay = currentDisplay as UserControls.Settings;
            if (settingsDisplay != null) {

                if (settingsDisplay.base_textbox.Text.Equals("") || settingsDisplay.torrent_textbox.Text.Equals("") ||
                    settingsDisplay.download_textbox.Text.Equals(""))
                    MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

                else {
                    string[] subgroups = settingsDisplay.subgroups_textbox.Text.Split(new string[] { ", " },
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
                                    new XElement("base", settingsDisplay.base_textbox.Text),
                                    new XElement("utorrent", settingsDisplay.download_textbox.Text),
                                    new XElement("torrents", settingsDisplay.torrent_textbox.Text)),
                                subgroup,
                                new XElement("flag",
                                    new XElement("only-whitelisted-subs", settingsDisplay.only_whitelisted_checkbox.IsChecked)))
                            );

                    doc.Save(settings.settingsXMLPath);
                    initializeSettings();
                }
            }
        }

        /// <summary>
        /// The view to create a new settings profile.
        /// </summary>
        private void newSettings() {
            changeDisplay(new UserControls.Settings());

            var settingsDisplay = currentDisplay as UserControls.Settings;
            if (settings != null) {
                toggleButtons(button_home, button_list, button_settings, button_check, button_folder, button_playlist, button_open_executing);
                settingsDisplay.base_textbox.Text = Directory.GetCurrentDirectory();
                settingsDisplay.torrent_textbox.Text = System.IO.Path.Combine(settingsDisplay.base_textbox.Text, "torrents");
                settingsDisplay.download_textbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
                settingsDisplay.apply_changes_button.Content = "Create Profile";
                settingsDisplay.apply_changes_button.Click += new RoutedEventHandler((object o, RoutedEventArgs e2) => {

                    if (settingsDisplay.base_textbox.Text.Equals("")        
                            || settingsDisplay.torrent_textbox.Text.Equals("") 
                            || settingsDisplay.download_textbox.Text.Equals(""))
                        MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

                    else {
                        settings = new Settings {
                            baseFolderPath = settingsDisplay.base_textbox.Text,
                            torrentFilesPath = settingsDisplay.torrent_textbox.Text,
                            utorrentPath = settingsDisplay.download_textbox.Text,
                            subgroups =
                                settingsDisplay.subgroups_textbox.Text.Split(new string[] {" "},
                                    StringSplitOptions.RemoveEmptyEntries),
                            onlyWhitelisted = settingsDisplay.only_whitelisted_checkbox.IsChecked.Value
                        };
                        createSettingsXML(settings);
                        toggleButtons(button_home, button_list, button_settings, button_check, 
                                      button_folder, button_playlist, button_open_executing);
                        initializeSettings();
                        pressButton(button_home);
                    }
                });
            }
        }

        /// <summary>
        /// The view to check for new anime and download it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_check_Click(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(settings.baseFolderPath))
                MessageBox.Show("Your base folder doesn't seem to exist.");

            else if (!File.Exists(settings.utorrentPath) || !settings.utorrentPath.ToLower().EndsWith(".exe"))
                MessageBox.Show("Your uTorrent.exe path seems to be wrong.");

            else {
                if (!Directory.Exists(settings.torrentFilesPath))
                    Directory.CreateDirectory(settings.torrentFilesPath);
                
                changeDisplay(new UserControls.Download());
                var downloadDisplay = currentDisplay as UserControls.Download;

                XDocument animeXML = XDocument.Load(settings.animeXMLPath);
                Anime[] animes = animeXML.Element("anime").Elements()
                    .Select(x => new Anime(x))
                    .Where(a => a.airing == true)
                    .ToArray();

                var online = await NyaaIsOnline();
                if (!online)
                    downloadDisplay.textBox.Text = ">> Nyaa is currently offline. Try checking later.";
                else
                    if (downloadDisplay != null) {
                        downloadAnime(downloadDisplay.textBox, animes);
                }
            }
        }

        /// <summary>
        /// The view to add new anime to the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_new_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new UserControls.Add());
            var animeDisplay = currentDisplay as UserControls.Add;

            if (animeDisplay != null) {
                animeDisplay.add_button.Click += new RoutedEventHandler(button_add_Click);

                KeyEventHandler enterApply = new KeyEventHandler((object s, KeyEventArgs k) => {
                    if (k.Key == Key.Enter) {
                        animeDisplay.add_button.Focus();
                        pressButton(animeDisplay.add_button);
                    }
                });

                animeDisplay.name_textbox.KeyUp += enterApply;
                animeDisplay.episode_textbox.KeyUp += enterApply;
            }
        }

        /// <summary>
        /// The submission button event for the add anime view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = currentDisplay as UserControls.Add;

            if (animeDisplay != null) {

                if (animeDisplay.name_textbox.Text.Equals("") || animeDisplay.episode_textbox.Text.Equals("")) {
                    MessageBox.Show("There needs to be a name and/or episode.");
                }

                else { 
                    Anime newAnime = new Anime {
                        name = animeDisplay.name_textbox.Text,
                        episode = string.Format("{0:D2}", int.Parse(animeDisplay.episode_textbox.Text)),
                        status = animeDisplay.status_combobox.Text,
                        resolution = animeDisplay.resolution_combobox.Text,
                        airing = animeDisplay.airing_checkbox.IsChecked.Value,
                        nameStrict = animeDisplay.name_strict_checkbox.IsChecked.Value
                    };
                    
                    addAnime(newAnime);
                    pressButton(button_list);
                }
            }
        }

        /// <summary>
        /// The view to edit anime selected from the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_edit_Click(object sender, RoutedEventArgs e) {
            var tableDisplay = currentDisplay as UserControls.AnimeList;

            if (tableDisplay != null) {
                if (tableDisplay.dataGrid.SelectedCells.FirstOrDefault().IsValid) {
                    
                    XElement item = tableDisplay.dataGrid.SelectedCells[0].Item as XElement; //  .Items[0].ToString());

                    changeDisplay(new UserControls.Add());

                    var animeDisplay = currentDisplay as UserControls.Add;
                    animeDisplay.add_button.Content = "Edit";
                    animeDisplay.add_button.Click += new RoutedEventHandler(button_anime_edit_Click);

                    KeyEventHandler enterApply = new KeyEventHandler((object s, KeyEventArgs k) => {
                        if (k.Key == Key.Enter) {
                            animeDisplay.add_button.Focus();
                            pressButton(animeDisplay.add_button);
                        }
                    });

                    animeDisplay.name_textbox.KeyUp += enterApply;
                    animeDisplay.episode_textbox.KeyUp += enterApply;

                    animeDisplay.name_textbox.Text = item.Element("name").Value;
                    animeDisplay.episode_textbox.Text = item.Element("episode").Value;
                    animeDisplay.resolution_combobox.Text = item.Element("resolution").Value;
                    animeDisplay.status_combobox.Text = item.Element("status").Value;
                    animeDisplay.airing_checkbox.IsChecked = Boolean.Parse(item.Element("airing").Value);
                    animeDisplay.name_strict_checkbox.IsChecked = Boolean.Parse(item.Element("name-strict").Value);

                    currentlyEditedAnime = item.Element("name").Value;
                }
            }
        }

        /// <summary>
        /// The submission button event for the edit anime view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_anime_edit_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = currentDisplay as UserControls.Add;

            if (animeDisplay != null) {
                
                if (animeDisplay.name_textbox.Text.Equals("") || animeDisplay.episode_textbox.Text.Equals(""))
                    MessageBox.Show("There needs to be a name and/or episode.");

                else {
                    Anime editedAnime = new Anime {
                        name = animeDisplay.name_textbox.Text,
                        episode = string.Format("{0:D2}", int.Parse(animeDisplay.episode_textbox.Text)),
                        status = animeDisplay.status_combobox.Text,
                        resolution = animeDisplay.resolution_combobox.Text,
                        airing = animeDisplay.airing_checkbox.IsChecked.Value,
                        nameStrict = animeDisplay.name_strict_checkbox.IsChecked.Value
                    };

                    editAnime(currentlyEditedAnime, editedAnime);
                    pressButton(button_list);
                }
            }
        }

        /// <summary>
        /// A test event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click(object sender, RoutedEventArgs e) {
            setAnimeEpisodeTotalToLastKnown();
            updateTable();
        }
    }
}
