using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml.Linq;
using anime_downloader.Classes;
using anime_downloader.UserControls;
using Settings = anime_downloader.Classes.Settings;

namespace anime_downloader {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        ///     A collection of all the anime.
        /// </summary>
        private List<XElement> anime;

        /// <summary>
        ///     The current display on the right window pane.
        /// </summary>
        private UserControl currentDisplay;

        /// <summary>
        ///     A helper for modifying anime.
        /// </summary>
        private string currentlyEditedAnime;

        /// <summary>
        ///     An object to create the playlist with some customization.
        /// </summary>
        private Playlist playlist;

        /// <summary>
        ///     The path and user settings object.
        /// </summary>
        private Settings settings;

        /// <summary>
        ///     The object used to modify the XML documents.
        /// </summary>
        private XML xml;

        public MainWindow() {
            InitializeComponent();
            changeDisplay(new Home());
            initializeSettings();
            refreshAnime();
        }

        /// <summary>
        ///     Initialize and set the settings object.
        /// </summary>
        private void initializeSettings() {
            var applicationPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "anime-downloader");

            if (!Directory.Exists(applicationPath))
                Directory.CreateDirectory(applicationPath);

            var settingsXMLPath = Path.Combine(applicationPath, "settings.xml");

            if (!File.Exists(settingsXMLPath))
                newSettings();

            XML.verifySettingsXMLSchema(settingsXMLPath);

            var xmlSettings = XDocument.Load(settingsXMLPath).Root;

            settings = new Settings {
                applicationPath = applicationPath,
                animeXMLPath = Path.Combine(applicationPath, "anime.xml"),
                settingsXMLPath = settingsXMLPath,
                baseFolderPath = xmlSettings.Element("path").Element("base").Value,
                torrentFilesPath = xmlSettings.Element("path").Element("torrents").Value,
                utorrentPath = xmlSettings.Element("path").Element("utorrent").Value,
                subgroups = xmlSettings.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray(),
                onlyWhitelisted = bool.Parse(xmlSettings.Element("flag").Element("only-whitelisted-subs").Value),
                sortBy = xmlSettings.Element("sortBy").Value
            };

            xml = new XML(settings);

            if (!File.Exists(settings.animeXMLPath))
                xml.createAnimeXML();

            XML.verifyAnimeXMLSchema(settings.animeXMLPath);

            playlist = new Playlist(settings);
        }

        /// <summary>
        ///     Update values in the anime variable.
        /// </summary>
        private void refreshAnime() {
            anime = XDocument.Load(settings.animeXMLPath).Root.Elements()
                .AsParallel()
                .OrderBy(e => e.Element(settings.sortBy).Value)
                .ToList();
            if (currentDisplay is AnimeList)
                (currentDisplay as AnimeList).dataGrid.ItemsSource = anime;
        }

        // Helper functions

        /// <summary>
        ///     Attempt to download anime and display the results.
        /// </summary>
        /// <param name="textbox">The output box to display results to.</param>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        private async void downloadAnime(TextBox textbox, Anime[] animes) {
            string filepath, command;
            var changedAnime = new List<Anime>();
            var client = new WebClient();
            var totalDownloaded = 0;

            textbox.Text = ">> Searching for currently airing anime episodes ...\n";

            foreach (var anime in animes) {
                var nyaaLinks = await anime.getLinksToNextEpisode();

                if (nyaaLinks != null) {
                    foreach (var nyaa in nyaaLinks) {

                        // Most likely wrong torrent
                        if (anime.nameStrict) {
                            if (!anime.name.Equals(nyaa.strippedName(true))) {
                                continue;
                            }
                        }

                        // Not the right subgroup
                        if (!anime.preferredSubgroup.Equals("") &
                            !nyaa?.subgroup().Contains(anime.preferredSubgroup) ?? false) {
                            continue;
                        }

                        if (settings.onlyWhitelisted) {

                            // Nyaa listing with no subgroup in the title
                            if (!nyaa.hasSubgroup()) {
                                // textbox.AppendText($"Found result for {anime.name} with no subgroup. Skipping ...\n");
                                // scrolldownTextbox(textbox);
                                continue;
                            }

                            // Nyaa listing with wrong subgroup
                            if (!settings.subgroups.Contains(nyaa.subgroup())) {
                                // textbox.AppendText($"Found result for {anime.name} with non-whitelisted subgroup. Skipping ...\n");
                                // scrolldownTextbox(textbox);
                                continue;
                            }
                        }

                        textbox.AppendText($"Downloading '{anime.title()}' episode '{anime.nextEpisode()}'.\n");
                        scrolldownTextbox(textbox);
                        filepath = Path.Combine(settings.torrentFilesPath, nyaa.torrentName());

                        if (!File.Exists(filepath))
                            await client.DownloadFileTaskAsync(nyaa.link, filepath);

                        command = $"/DIRECTORY \"{getOutputFolder()}\" \"{filepath}\"";
                        callCommand(settings.utorrentPath, command);

                        anime.episode = anime.nextEpisode();
                        changedAnime.Add(anime);
                        totalDownloaded++;
                        break;
                    }
                }
            }

            xml.editAnime(changedAnime);
            refreshAnime();
            textbox.AppendText(totalDownloaded > 0
                ? $">> Found {totalDownloaded} anime downloads."
                : ">> No new anime found.");
            scrolldownTextbox(textbox);
            toggleButtons(button_home, button_list, button_settings, button_check, button_playlist);
        }

        /// <summary>
        ///     Check if Nyaa.se is online within 1.0 seconds so not to hang when entering download view.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> NyaaIsOnline() {
            var httpReq = (HttpWebRequest) WebRequest.Create("https://www.nyaa.se/");
            httpReq.Timeout = 1000;
            httpReq.AllowAutoRedirect = false;
            try {
                var httpRes = await Task.Run(() => (HttpWebResponse) httpReq.GetResponse());
                return httpRes.StatusCode == HttpStatusCode.OK;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        ///     Execute new process with given parameters.
        /// </summary>
        /// <param name="executable">Path to the executable file.</param>
        /// <param name="parameters">Arguments given to the executable.</param>
        private void callCommand(string executable, string parameters) {
            var proc = new Process();
            proc.StartInfo.FileName = executable;
            proc.StartInfo.Arguments = parameters;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            Task.Run(() => proc.Start());
        }

        /// <summary>
        ///     Create and return the path to a folder based on a timestamp of the current moment.
        /// </summary>
        /// <returns>A path used to download into.</returns>
        private string getOutputFolder() {
            var date = DateTime.Now;
            var week = Math.Floor(Convert.ToDouble(date.DayOfYear)/7);
            var folder = $"{date.Year} - Week {week} - {date.ToString("MMMM")}";
            var path = Path.Combine(settings.baseFolderPath, folder);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        /// <summary>
        ///     Toggle opacity and visibility of arbitrary amount of buttons.
        /// </summary>
        /// <param name="buttons">Any button element.</param>
        private void toggleButtons(params Button[] buttons) {
            foreach (var button in buttons) {
                if (button.IsHitTestVisible) {
                    button.IsHitTestVisible = false;
                    button.Opacity = 0.4;
                }
                else {
                    button.IsHitTestVisible = true;
                    button.Opacity = 1.0;
                }
            }
        }

        /// <summary>
        ///     Simulate a button press.
        /// </summary>
        /// <param name="button">The button to press.</param>
        private void pressButton(Button button) {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        /// <summary>
        ///     Change the current main display to another display.
        /// </summary>
        /// <param name="userDisplay">The user supplied display.</param>
        private void changeDisplay(UserControl userDisplay) {
            if (currentDisplay == null || currentDisplay.GetType() != userDisplay.GetType()) {
                display.Children.Clear();
                currentDisplay = userDisplay;
                display.Children.Add(currentDisplay);
            }
        }

        /// <summary>
        ///     Strip the video name of all tags (resolution, seeders, etc).
        /// </summary>
        /// <param name="name">A downloaded file's name.</param>
        /// <returns></returns>
        private string stripFilename(string name) {
            var pattern = @"(\[(?:.*?)\])|(\((?:.*)\))";
            var scrubbedName = name;
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
        ///     Returns a collection of [{animeName: lastGivenEpisode}] from a list of stripped titles.
        /// </summary>
        /// <param name="strippedNames">A collection of names passed through stripFilename().</param>
        /// <returns></returns>
        private Dictionary<string, int> collectLastEpisode(string[] strippedNames) {
            var latest = new Dictionary<string, int>();

            foreach (var name in strippedNames) {
                var animeName = string.Join(" - ",
                    name.Split(new[] {" .mp4", " .mkv"}, StringSplitOptions.RemoveEmptyEntries)[0]
                        .Split(new[] {" - "}, StringSplitOptions.RemoveEmptyEntries)
                        .TakeWhile(s => !s.All(c => char.IsNumber(c))));
                var animeEpisode = int.Parse(name.Split('-').Last().Split()[1]);

                if (!latest.ContainsKey(animeName))
                    latest.Add(animeName, animeEpisode);
                else if (latest[animeName] < animeEpisode)
                    latest[animeName] = animeEpisode;
            }

            return latest;
        }

        /// <summary>
        ///     Set all anime episode counts in the anime list to their last known values from the "watched" folder.
        /// </summary>
        /// <remarks>This is for re-indexing if you don't know which episodes you watched last.</remarks>
        private void setAnimeEpisodeTotalToLastKnown() {
            var path = Path.Combine(settings.baseFolderPath, "watched");

            var finishedAnimes = collectLastEpisode(Directory.GetFiles(path)
                .Select(f => Path.GetFileName(f))
                .Select(n => stripFilename(n))
                .ToArray());
            
            // I'm not sure how to cleanly turn this into a generic XML function above.
            var document = XDocument.Load(settings.animeXMLPath);
            var root = document.Root;

            foreach (var entry in finishedAnimes) {
                var selected = root.Elements()
                    .Where(a => entry.Key.ToLower().Contains(a.Element("name").Value.ToLower()))
                    .FirstOrDefault();
                if (selected != null)
                    selected.Element("episode").Value = string.Format("{0:D2}", entry.Value);
            }

            document.Save(settings.animeXMLPath);
        }

        /// <summary>
        ///     Scroll to the bottom of a textbox.
        /// </summary>
        /// <param name="textbox">The textbox that will be scrolled down in.</param>
        private void scrolldownTextbox(TextBox textbox) {
            textbox.Focus();
            textbox.CaretIndex = textbox.Text.Length;
            textbox.ScrollToEnd();
        }

        // Event handling

        /// <summary>
        ///     The home view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_home_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new Home());
        }

        private void button_folder_Click(object sender, RoutedEventArgs e) {
            if (Directory.Exists(settings.baseFolderPath))
                Process.Start(settings.baseFolderPath);
            else
                MessageBox.Show("Your base folder doesn't seem to exist.");
        }

        private void button_playlist_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new PlaylistCreator());
            (currentDisplay as PlaylistCreator).button.Click += playlist_button_Click;
        }

        private void playlist_button_Click(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(settings.baseFolderPath))
                MessageBox.Show("Your base folder doesn't seem to exist.");

            else {
                playlist.refresh();

                var playlistCreatorDisplay = currentDisplay as PlaylistCreator;

                if (playlistCreatorDisplay.episode_radio.IsChecked.Value)
                    playlist.byEpisodeNumber();

                else if (playlistCreatorDisplay.moment_radio.IsChecked.Value)
                    playlist.byDate();

                // else pass

                if (playlistCreatorDisplay.seperate_checkBox.IsChecked.Value)
                    playlist.separateShowOrder();

                playlist.save();
            }
        }

        private void button_open_executing_Click(object sender, RoutedEventArgs e) {
            Process.Start(settings.applicationPath);
        }

        /// <summary>
        ///     The view to display the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_list_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new AnimeList());
            var animeListDisplay = currentDisplay as AnimeList;
            animeListDisplay.add.Click += button_add_new_Click;
            animeListDisplay.edit.Click += anime_list_edit_Click;
            animeListDisplay.delete.Click += anime_list_delete_Click;
            animeListDisplay.dataGrid.PreviewKeyDown += anime_list_delete_KeyDown;
            animeListDisplay.dataGrid.MouseDoubleClick += anime_list_MouseDoubleClick;
            animeListDisplay.dataGrid.ItemsSource = anime;
        }

        /// <summary>
        ///     The context menu event for the anime list view, selection: "Delete"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_delete_Click(object sender, RoutedEventArgs e) {
            var animeListDisplay = currentDisplay as AnimeList;
            var selected = animeListDisplay.dataGrid.SelectedCells.FirstOrDefault();

            if (selected.IsValid) {
                var row = selected.Item as XElement;
                if (row != null) {
                    xml.removeAnime(row.Element("name").Value);
                    refreshAnime();
                }
            }
        }

        /// <summary>
        ///     The keydown event for the anime list view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_delete_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Delete) {
                var animeListDisplay = currentDisplay as AnimeList;
                var row = animeListDisplay.dataGrid.SelectedCells[0].Item as XElement;
                xml.removeAnime(row.Element("name").Value);
                refreshAnime();
            }
        }

        /// <summary>
        ///     The double click event for the anime list view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_MouseDoubleClick(object sender, MouseEventArgs e) {
            var animeListDisplay = currentDisplay as AnimeList;
            var selected = animeListDisplay.dataGrid.SelectedCells.FirstOrDefault();
            if (selected.IsValid) {
                anime_list_edit_Click(sender, e);
            }
        }

        /// <summary>
        ///     The view to manage settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_settings_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new UserControls.Settings());

            if (settings != null) {
                var settingsDisplay = currentDisplay as UserControls.Settings;
                settingsDisplay.base_textbox.Text = settings.baseFolderPath;
                settingsDisplay.subgroups_textbox.Text = string.Join(", ", settings.subgroups);
                settingsDisplay.download_textbox.Text = settings.utorrentPath;
                settingsDisplay.torrent_textbox.Text = settings.torrentFilesPath;
                settingsDisplay.apply_changes_button.Click += button_apply_settings_Click;
                settingsDisplay.only_whitelisted_checkbox.IsChecked = settings.onlyWhitelisted;
            }
        }

        /// <summary>
        ///     The submission button event for the edit settings view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_apply_settings_Click(object sender, RoutedEventArgs e) {
            var settingsDisplay = currentDisplay as UserControls.Settings;

            if (settingsDisplay.base_textbox.Text.Equals("") || settingsDisplay.torrent_textbox.Text.Equals("") ||
                settingsDisplay.download_textbox.Text.Equals(""))
                MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

            else {
                var subgroups = settingsDisplay.subgroups_textbox.Text.Split(new[] {", "},
                    StringSplitOptions.None);

                var subgroup = new XElement("subgroup");
                foreach (var sub in subgroups)
                    subgroup.Add(new XElement("name", sub));

                var doc =
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
                                new XElement("only-whitelisted-subs",
                                    settingsDisplay.only_whitelisted_checkbox.IsChecked)))
                        );

                doc.Save(settings.settingsXMLPath);
                initializeSettings();
            }
        }

        /// <summary>
        ///     The view to create a new settings profile.
        /// </summary>
        private void newSettings() {
            changeDisplay(new UserControls.Settings());

            if (settings != null) {
                var settingsDisplay = currentDisplay as UserControls.Settings;
                toggleButtons(button_home, button_list, button_settings, button_check, button_folder, button_playlist,
                    button_open_executing);
                settingsDisplay.base_textbox.Text = Directory.GetCurrentDirectory();
                settingsDisplay.torrent_textbox.Text = Path.Combine(settingsDisplay.base_textbox.Text, "torrents");
                settingsDisplay.download_textbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
                settingsDisplay.apply_changes_button.Content = "Create Profile";
                settingsDisplay.apply_changes_button.Click += (object o, RoutedEventArgs e2) => {
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
                                settingsDisplay.subgroups_textbox.Text.Split(new[] {" "},
                                    StringSplitOptions.RemoveEmptyEntries),
                            onlyWhitelisted = settingsDisplay.only_whitelisted_checkbox.IsChecked.Value,
                            sortBy = "name"
                        };
                        xml.createSettingsXML(settings);
                        toggleButtons(button_home, button_list, button_settings, button_check,
                            button_folder, button_playlist, button_open_executing);
                        initializeSettings();
                        pressButton(button_home);
                    }
                };
            }
        }

        /// <summary>
        ///     The view to check for new anime and download it.
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

                changeDisplay(new Download());
                var downloadDisplay = currentDisplay as Download;

                var animeXML = XDocument.Load(settings.animeXMLPath);
                var animes = animeXML.Element("anime").Elements()
                    .Select(x => new Anime(x))
                    .Where(a => a.airing && a.status == "Watching")
                    .ToArray();

                toggleButtons(button_home, button_list, button_settings, button_check, button_playlist);

                var online = await NyaaIsOnline();

                if (!online) {
                    downloadDisplay.textBox.Text = ">> Nyaa is currently offline. Try checking later.";
                    toggleButtons(button_home, button_list, button_settings, button_check, button_playlist);
                }

                else if (downloadDisplay != null) {
                    downloadAnime(downloadDisplay.textBox, animes);
                }
            }
        }

        /// <summary>
        ///     The view to add new anime to the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_new_Click(object sender, RoutedEventArgs e) {
            changeDisplay(new Add());
            var animeDisplay = currentDisplay as Add;
            animeDisplay.add_button.Click += button_add_Click;

            KeyEventHandler enterApply = (object s, KeyEventArgs k) => {
                if (k.Key == Key.Enter) {
                    animeDisplay.add_button.Focus();
                    pressButton(animeDisplay.add_button);
                }
            };

            animeDisplay.name_textbox.KeyUp += enterApply;
            animeDisplay.episode_textbox.KeyUp += enterApply;
            settings.subgroups.ToList().ForEach(s => animeDisplay.subgroup_comboBox.Items.Add(s));
        }

        /// <summary>
        ///     The submission button event for the add anime view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = currentDisplay as Add;

            if (animeDisplay.name_textbox.Text.Equals("") || animeDisplay.episode_textbox.Text.Equals("")) {
                MessageBox.Show("There needs to be a name and/or episode.");
            }

            else {
                var subgroup = animeDisplay.subgroup_comboBox.Text;
                if (subgroup.Equals("(None)"))
                    subgroup = "";

                var newAnime = new Anime {
                    name = animeDisplay.name_textbox.Text,
                    episode = string.Format("{0:D2}", int.Parse(animeDisplay.episode_textbox.Text)),
                    status = animeDisplay.status_combobox.Text,
                    resolution = animeDisplay.resolution_combobox.Text,
                    airing = animeDisplay.airing_checkbox.IsChecked.Value,
                    nameStrict = animeDisplay.name_strict_checkbox.IsChecked.Value,
                    preferredSubgroup = subgroup
                };

                xml.addAnime(newAnime);
                refreshAnime();
                pressButton(button_list);
            }
        }

        /// <summary>
        ///     The view to edit anime selected from the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_edit_Click(object sender, RoutedEventArgs e) {
            var tableDisplay = currentDisplay as AnimeList;
            var item = tableDisplay.dataGrid.SelectedCells[0].Item as XElement; //  .Items[0].ToString());

            changeDisplay(new Add());

            var animeDisplay = currentDisplay as Add;
            animeDisplay.add_button.Content = "Edit";
            animeDisplay.add_button.Click += button_anime_edit_Click;

            KeyEventHandler enterApply = (object s, KeyEventArgs k) => {
                if (k.Key == Key.Enter) {
                    animeDisplay.add_button.Focus();
                    pressButton(animeDisplay.add_button);
                }
            };

            var subgroup = item.Element("preferredSubgroup").Value;

            animeDisplay.name_textbox.KeyUp += enterApply;
            animeDisplay.episode_textbox.KeyUp += enterApply;

            animeDisplay.name_textbox.Text = item.Element("name").Value;
            animeDisplay.episode_textbox.Text = item.Element("episode").Value;
            animeDisplay.resolution_combobox.Text = item.Element("resolution").Value;
            animeDisplay.status_combobox.Text = item.Element("status").Value;
            animeDisplay.airing_checkbox.IsChecked = bool.Parse(item.Element("airing").Value);
            animeDisplay.name_strict_checkbox.IsChecked = bool.Parse(item.Element("name-strict").Value);

            settings.subgroups.ToList().ForEach(s => animeDisplay.subgroup_comboBox.Items.Add(s));
            animeDisplay.subgroup_comboBox.Text = subgroup.Equals("") ? "(None)" : subgroup;

            currentlyEditedAnime = item.Element("name").Value;
        }

        /// <summary>
        ///     The submission button event for the edit anime view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_anime_edit_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = currentDisplay as Add;

            if (animeDisplay.name_textbox.Text.Equals("") || animeDisplay.episode_textbox.Text.Equals(""))
                MessageBox.Show("There needs to be a name and/or episode.");

            else {
                var subgroup = animeDisplay.subgroup_comboBox.Text;

                var editedAnime = new Anime {
                    name = animeDisplay.name_textbox.Text,
                    episode = string.Format("{0:D2}", int.Parse(animeDisplay.episode_textbox.Text)),
                    status = animeDisplay.status_combobox.Text,
                    resolution = animeDisplay.resolution_combobox.Text,
                    airing = animeDisplay.airing_checkbox.IsChecked.Value,
                    nameStrict = animeDisplay.name_strict_checkbox.IsChecked.Value,
                    preferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup
                };

                xml.editAnime(currentlyEditedAnime, editedAnime);
                refreshAnime();
                pressButton(button_list);
            }
        }

        /// <summary>
        ///     A test event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click(object sender, RoutedEventArgs e) {
            setAnimeEpisodeTotalToLastKnown();
            refreshAnime();
        }
    }
}