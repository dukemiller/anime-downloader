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
        private List<XElement> _allAnime;

        /// <summary>
        ///     The current display on the right window pane.
        /// </summary>
        private UserControl _currentDisplay;

        /// <summary>
        ///     A helper for modifying anime.
        /// </summary>
        private string _currentlyEditedAnime;

        /// <summary>
        ///     An object to create the playlist with some customization.
        /// </summary>
        private Playlist _playlist;

        /// <summary>
        ///     The path and user settings object.
        /// </summary>
        private Settings _settings;

        /// <summary>
        ///     The object used to modify the XML documents.
        /// </summary>
        private Xml _xml;

        public MainWindow() {
            InitializeComponent();
            ChangeDisplay(new Home());
            InitializeSettings();
            RefreshAnime();
        }

        /// <summary>
        ///     Initialize and set the settings object.
        /// </summary>
        private void InitializeSettings() {
            _settings = new Settings();
            _xml = new Xml(_settings);
            _playlist = new Playlist(_settings);

            _settings.ApplicationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "anime-downloader");

            if (!Directory.Exists(_settings.ApplicationPath))
                Directory.CreateDirectory(_settings.ApplicationPath);
            
            if (!File.Exists(_settings.SettingsXmlPath))
                NewSettings();

            if (!File.Exists(_settings.AnimeXmlPath))
                _xml.CreateAnimeXml();

            Xml.VerifySettingsXmlSchema(_settings.SettingsXmlPath);
            Xml.VerifyAnimeXmlSchema(_settings.AnimeXmlPath);

            var xmlSettings = XDocument.Load(_settings.SettingsXmlPath).Root;

            if (xmlSettings != null) {
                _settings.BaseFolderPath = xmlSettings.Element("path")?.Element("base")?.Value;
                _settings.TorrentFilesPath = xmlSettings.Element("path")?.Element("torrents")?.Value;
                _settings.Subgroups = xmlSettings.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray();
                _settings.UtorrentPath = xmlSettings.Element("path")?.Element("utorrent")?.Value;
                _settings.OnlyWhitelisted = bool.Parse(xmlSettings.Element("flag")?.Element("only-whitelisted-subs")?.Value ?? "false");
                _settings.SortBy = xmlSettings.Element("sortBy")?.Value;
            }
        }

        /// <summary>
        ///     Update values in the anime variable.
        /// </summary>
        private void RefreshAnime() {
            _allAnime = XDocument.Load(_settings.AnimeXmlPath).Root?.Elements()
                .AsParallel()
                .OrderBy(e => e.Element(_settings.SortBy)?.Value)
                .ToList();
            var animeListDisplay = _currentDisplay as AnimeList;
            if (animeListDisplay != null && _allAnime != null) 
                animeListDisplay.DataGrid.ItemsSource = _allAnime;
        }

        // Helper functions

        /// <summary>
        ///     Attempt to download anime and display the results.
        /// </summary>
        /// <param name="textbox">The output box to display results to.</param>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        private async void DownloadAnime(TextBox textbox, Anime[] animes) {
            var changedAnime = new List<Anime>();
            var client = new WebClient();
            var totalDownloaded = 0;

            textbox.Text = ">> Searching for currently airing anime episodes ...\n";

            foreach (var anime in animes) {
                var nyaaLinks = await anime.GetLinksToNextEpisode();

                foreach (var nyaa in nyaaLinks) {

                    // Most likely wrong torrent
                    if (anime.NameStrict && !anime.Name.Equals(nyaa.StrippedName(true)))
                        continue;

                    // Not the right subgroup
                    if (!anime.PreferredSubgroup.Equals("") & !nyaa?.Subgroup().Contains(anime.PreferredSubgroup) ?? false)
                        continue;

                    if (_settings.OnlyWhitelisted) {

                        // Nyaa listing with no subgroup in the title
                        if (!nyaa?.HasSubgroup() ?? false) {
                            // textbox.AppendText($"Found result for {anime.name} with no subgroup. Skipping ...\n");
                            // scrolldownTextbox(textbox);
                            continue;
                        }

                        // Nyaa listing with wrong subgroup
                        if (!_settings.Subgroups.Contains(nyaa?.Subgroup())) {
                            // textbox.AppendText($"Found result for {anime.name} with non-whitelisted subgroup. Skipping ...\n");
                            // scrolldownTextbox(textbox);
                            continue;
                        }
                    }

                    textbox.AppendText($"Downloading '{anime.Title()}' episode '{anime.NextEpisode()}'.\n");
                    scrolldownTextbox(textbox);
                    var filepath = Path.Combine(_settings?.TorrentFilesPath, nyaa?.TorrentName());

                    if (!File.Exists(filepath))
                        await client.DownloadFileTaskAsync(nyaa?.Link, filepath);

                    var command = $"/DIRECTORY \"{GetOutputFolder()}\" \"{filepath}\"";
                    callCommand(_settings.UtorrentPath, command);

                    anime.Episode = anime.NextEpisode();
                    changedAnime.Add(anime);
                    totalDownloaded++;
                    break;
                }
            }

            _xml.EditAnime(changedAnime);
            RefreshAnime();
            textbox.AppendText(totalDownloaded > 0
                ? $">> Found {totalDownloaded} anime downloads."
                : ">> No new anime found.");
            scrolldownTextbox(textbox);
            toggleButtons(ButtonHome, ButtonList, ButtonSettings, ButtonCheck, ButtonPlaylist);
        }

        /// <summary>
        ///     Check if Nyaa.se is online within 1.0 seconds so not to hang when entering download view.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> NyaaIsOnline() {
            var httpWebRequest = WebRequest.Create("https://www.nyaa.se/") as HttpWebRequest;

            if (httpWebRequest != null) {
                httpWebRequest.Timeout = 1000;
                httpWebRequest.AllowAutoRedirect = false;

                try {
                    var httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse;
                    return httpWebResponse?.StatusCode == HttpStatusCode.OK;
                }

                catch {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        ///     Execute new process with given parameters.
        /// </summary>
        /// <param name="executable">Path to the executable file.</param>
        /// <param name="parameters">Arguments given to the executable.</param>
        private void callCommand(string executable, string parameters) {
            ProcessStartInfo info = new ProcessStartInfo() {
                FileName = executable,
                Arguments = parameters,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = new Process() {
                StartInfo = info
            };

            Task.Run(() => process.Start());
        }

        /// <summary>
        ///     Create and return the path to a folder based on a timestamp of the current moment.
        /// </summary>
        /// <returns>A path used to download into.</returns>
        private string GetOutputFolder() {
            var date = DateTime.Now;
            var week = Math.Floor(Convert.ToDouble(date.DayOfYear)/7);
            var folder = $"{date.Year} - Week {week} - {date.ToString("MMMM")}";
            var path = Path.Combine(_settings.BaseFolderPath, folder);

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
        private void ChangeDisplay(UserControl userDisplay) {
            if (_currentDisplay == null || _currentDisplay.GetType() != userDisplay.GetType()) {
                Display.Children.Clear();
                _currentDisplay = userDisplay;
                Display.Children.Add(_currentDisplay);
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
                    var m = match.Groups[1].Value ?? match.Groups[2].Value ?? "";
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
                        .TakeWhile(s => !s.All(char.IsNumber)));
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
        private void SetAnimeEpisodeTotalToLastKnown() {
            var path = Path.Combine(_settings.BaseFolderPath, "watched");

            var finishedAnimes = collectLastEpisode(Directory.GetFiles(path)
                .AsParallel()
                .Select(Path.GetFileName)
                .Select(stripFilename)
                .ToArray());
            
            // I'm not sure how to cleanly turn this into a generic XML function above.
            var document = XDocument.Load(_settings.AnimeXmlPath);
            var root = document.Root;

            if (root != null) {
                foreach (var entry in finishedAnimes) {
                    var selected = root.Elements()
                        .AsParallel()
                        .Where(a => entry.Key?.ToLower().Contains(a.Element("name")?.Value.ToLower()) ?? false)
                        .FirstOrDefault();
                    if (selected != null)
                        selected.SetElementValue("episode", $"{entry.Value:D2}");
                }
            }

            document.Save(_settings.AnimeXmlPath);
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
            ChangeDisplay(new Home());
        }

        private void button_folder_Click(object sender, RoutedEventArgs e) {
            if (Directory.Exists(_settings.BaseFolderPath))
                Process.Start(_settings.BaseFolderPath);
            else
                MessageBox.Show("Your base folder doesn't seem to exist.");
        }

        private void button_playlist_Click(object sender, RoutedEventArgs e) {
            ChangeDisplay(new PlaylistCreator());
            var playlistDisplay = _currentDisplay as PlaylistCreator;
            if (playlistDisplay != null) {
                playlistDisplay.CreateButton.Click += PlaylistCreateButtonClick;
            }
        }

        private void PlaylistCreateButtonClick(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(_settings.BaseFolderPath))
                MessageBox.Show("Your base folder doesn't seem to exist.");
            
            else {
                _playlist.Refresh();

                var playlistCreatorDisplay = _currentDisplay as PlaylistCreator;

                if (playlistCreatorDisplay != null) {

                    if (playlistCreatorDisplay.EpisodeRadio.IsChecked ?? false)
                        _playlist.ByEpisodeNumber();

                    else if (playlistCreatorDisplay.MomentRadio.IsChecked ?? false)
                        _playlist.ByDate();

                    // else pass

                    if (playlistCreatorDisplay.SeperateCheckBox.IsChecked ?? false)
                        _playlist.SeparateShowOrder();

                    _playlist.Save();
                }
            }
        }

        private void button_open_executing_Click(object sender, RoutedEventArgs e) {
            Process.Start(_settings.ApplicationPath);
        }

        /// <summary>
        ///     The view to display the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_list_Click(object sender, RoutedEventArgs e) {
            ChangeDisplay(new AnimeList());
            var animeListDisplay = _currentDisplay as AnimeList;
            if (animeListDisplay != null) {
                animeListDisplay.Add.Click += button_add_new_Click;
                animeListDisplay.Edit.Click += anime_list_edit_Click;
                animeListDisplay.Delete.Click += anime_list_delete_Click;
                animeListDisplay.DataGrid.PreviewKeyDown += anime_list_delete_KeyDown;
                animeListDisplay.DataGrid.MouseDoubleClick += anime_list_MouseDoubleClick;
                animeListDisplay.DataGrid.ItemsSource = _allAnime;
            }
        }

        /// <summary>
        ///     The context menu event for the anime list view, selection: "Delete"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_delete_Click(object sender, RoutedEventArgs e) {
            var animeListDisplay = _currentDisplay as AnimeList;
            if (animeListDisplay != null) {
                var selected = animeListDisplay.DataGrid.SelectedCells.FirstOrDefault();
                if (selected.IsValid) {
                    var row = selected.Item as XElement;
                    if (row != null) {
                        _xml.RemoveAnime(row.Element("name")?.Value);
                        RefreshAnime();
                    }
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
                var animeListDisplay = _currentDisplay as AnimeList;
                if (animeListDisplay != null) {
                    var row = animeListDisplay.DataGrid?.SelectedCells?[0].Item as XElement;
                    _xml.RemoveAnime(row?.Element("name")?.Value);
                    RefreshAnime();
                }
            }
        }

        /// <summary>
        ///     The double click event for the anime list view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_MouseDoubleClick(object sender, MouseEventArgs e) {
            var animeListDisplay = _currentDisplay as AnimeList;
            if (animeListDisplay != null) {
                var selected = animeListDisplay.DataGrid.SelectedCells.FirstOrDefault();
                if (selected.IsValid) {
                    anime_list_edit_Click(sender, e);
                }
            }
        }

        /// <summary>
        ///     The view to manage settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_settings_Click(object sender, RoutedEventArgs e) {
            ChangeDisplay(new UserControls.Settings());

            if (_settings != null) {
                var settingsDisplay = _currentDisplay as UserControls.Settings;
                if (settingsDisplay != null) {
                    settingsDisplay.BaseTextbox.Text = _settings.BaseFolderPath;
                    settingsDisplay.SubgroupsTextbox.Text = string.Join(", ", _settings.Subgroups);
                    settingsDisplay.DownloadTextbox.Text = _settings.UtorrentPath;
                    settingsDisplay.TorrentTextbox.Text = _settings.TorrentFilesPath;
                    settingsDisplay.ApplyChangesButton.Click += button_apply_settings_Click;
                    settingsDisplay.OnlyWhitelistedCheckbox.IsChecked = _settings.OnlyWhitelisted;
                }
            }
        }

        /// <summary>
        ///     The submission button event for the edit settings view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_apply_settings_Click(object sender, RoutedEventArgs e) {
            var settingsDisplay = _currentDisplay as UserControls.Settings;

            if (settingsDisplay != null) {
                if (settingsDisplay.BaseTextbox.Text.Equals("") || settingsDisplay.TorrentTextbox.Text.Equals("") ||
                    settingsDisplay.DownloadTextbox.Text.Equals(""))
                    MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

                else {
                    var subgroups = settingsDisplay.SubgroupsTextbox.Text.Split(new[] {", "},
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
                                    new XElement("base", settingsDisplay.BaseTextbox.Text),
                                    new XElement("utorrent", settingsDisplay.DownloadTextbox.Text),
                                    new XElement("torrents", settingsDisplay.TorrentTextbox.Text)),
                                subgroup,
                                new XElement("flag",
                                    new XElement("only-whitelisted-subs",
                                        settingsDisplay.OnlyWhitelistedCheckbox.IsChecked)))
                            );

                    doc.Save(_settings.SettingsXmlPath);
                    InitializeSettings();
                }
            }
        }

        /// <summary>
        ///     The view to create a new settings profile.
        /// </summary>
        private void NewSettings() {
            ChangeDisplay(new UserControls.Settings());

            if (_settings != null) {
                var settingsDisplay = _currentDisplay as UserControls.Settings;

                if (settingsDisplay != null) {
                    toggleButtons(ButtonHome, ButtonList, ButtonSettings, ButtonCheck, ButtonFolder, ButtonPlaylist,
                        ButtonOpenExecuting);
                    settingsDisplay.BaseTextbox.Text = Directory.GetCurrentDirectory();
                    settingsDisplay.TorrentTextbox.Text = Path.Combine(settingsDisplay.BaseTextbox.Text, "torrents");
                    settingsDisplay.DownloadTextbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
                    settingsDisplay.ApplyChangesButton.Content = "Create Profile";

                    settingsDisplay.ApplyChangesButton.Click += (obj, ev) => {
                        if (settingsDisplay.BaseTextbox.Text.Equals("")
                            || settingsDisplay.TorrentTextbox.Text.Equals("")
                            || settingsDisplay.DownloadTextbox.Text.Equals(""))
                            MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

                        else {
                            _settings = new Settings {
                                BaseFolderPath = settingsDisplay.BaseTextbox.Text,
                                TorrentFilesPath = settingsDisplay.TorrentTextbox.Text,
                                UtorrentPath = settingsDisplay.DownloadTextbox.Text,
                                Subgroups =
                                    settingsDisplay.SubgroupsTextbox.Text.Split(new[] {" "},
                                        StringSplitOptions.RemoveEmptyEntries),
                                OnlyWhitelisted = settingsDisplay.OnlyWhitelistedCheckbox.IsChecked ?? false,
                                SortBy = "name"
                            };
                            _xml.CreateSettingsXml(_settings);
                            toggleButtons(ButtonHome, ButtonList, ButtonSettings, ButtonCheck,
                                ButtonFolder, ButtonPlaylist, ButtonOpenExecuting);
                            InitializeSettings();
                            pressButton(ButtonHome);
                        }
                    };
                }
            }
        }

        /// <summary>
        ///     The view to check for new anime and download it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_check_Click(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(_settings.BaseFolderPath))
                MessageBox.Show("Your base folder doesn't seem to exist.");

            else if (!File.Exists(_settings.UtorrentPath) || !_settings.UtorrentPath.ToLower().EndsWith(".exe"))
                MessageBox.Show("Your uTorrent.exe path seems to be wrong.");

            else {
                if (!Directory.Exists(_settings.TorrentFilesPath))
                    Directory.CreateDirectory(_settings.TorrentFilesPath);

                ChangeDisplay(new Download());
                var downloadDisplay = _currentDisplay as Download;

                var animeXml = XDocument.Load(_settings.AnimeXmlPath);
                var animes = animeXml.Element("anime")?.Elements()
                    .AsParallel()
                    .Select(x => new Anime(x))
                    .Where(a => a.Airing && a.Status == "Watching")
                    .ToArray();

                toggleButtons(ButtonHome, ButtonList, ButtonSettings, ButtonCheck, ButtonPlaylist);

                var online = await NyaaIsOnline();

                if (downloadDisplay != null) {
                    if (!online) {
                        downloadDisplay.TextBox.Text = ">> Nyaa is currently offline. Try checking later.";
                        toggleButtons(ButtonHome, ButtonList, ButtonSettings, ButtonCheck, ButtonPlaylist);
                    }

                    else {
                        DownloadAnime(downloadDisplay.TextBox, animes);
                    }
                }
            }
        }

        /// <summary>
        ///     The view to add new anime to the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_new_Click(object sender, RoutedEventArgs e) {
            ChangeDisplay(new Add());
            var animeDisplay = _currentDisplay as Add;

            if (animeDisplay != null) {
                animeDisplay.AddButton.Click += button_add_Click;

                KeyEventHandler enterApply = (obj, k) => {
                    if (k.Key == Key.Enter) {
                        animeDisplay.AddButton.Focus();
                        pressButton(animeDisplay.AddButton);
                    }
                };

                animeDisplay.NameTextbox.KeyUp += enterApply;
                animeDisplay.EpisodeTextbox.KeyUp += enterApply;
                _settings.Subgroups.ToList().ForEach(s => animeDisplay.SubgroupComboBox.Items.Add(s));
            }
        }

        /// <summary>
        ///     The submission button event for the add anime view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = _currentDisplay as Add;

            if (animeDisplay != null) {
                if (animeDisplay.NameTextbox.Text.Equals("") || animeDisplay.EpisodeTextbox.Text.Equals("")) {
                    MessageBox.Show("There needs to be a name and/or episode.");
                }

                else {
                    var subgroup = animeDisplay.SubgroupComboBox.Text;
                    if (subgroup.Equals("(None)"))
                        subgroup = "";

                    var newAnime = new Anime {
                        Name = animeDisplay.NameTextbox.Text,
                        Episode = $"{int.Parse(animeDisplay.EpisodeTextbox.Text):D2}",
                        Status = animeDisplay.StatusCombobox.Text,
                        Resolution = animeDisplay.ResolutionCombobox.Text,
                        Airing = animeDisplay.AiringCheckbox.IsChecked ?? false,
                        NameStrict = animeDisplay.NameStrictCheckbox.IsChecked ?? false,
                        PreferredSubgroup = subgroup
                    };

                    _xml.AddAnime(newAnime);
                    RefreshAnime();
                    pressButton(ButtonList);
                }
            }
        }

        /// <summary>
        ///     The view to edit anime selected from the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void anime_list_edit_Click(object sender, RoutedEventArgs e) {
            var tableDisplay = _currentDisplay as AnimeList;

            if (tableDisplay != null) {
                var item = tableDisplay.DataGrid.SelectedCells[0].Item as XElement; //  .Items[0].ToString());

                ChangeDisplay(new Add());

                var animeDisplay = _currentDisplay as Add;

                if (animeDisplay != null) {
                    animeDisplay.AddButton.Content = "Edit";
                    animeDisplay.AddButton.Click += button_anime_edit_Click;

                    KeyEventHandler enterApply = (obj, k) => {
                        if (k.Key == Key.Enter) {
                            animeDisplay.AddButton.Focus();
                            pressButton(animeDisplay.AddButton);
                        }
                    };

                    var subgroup = item.Element("preferredSubgroup").Value;

                    animeDisplay.NameTextbox.KeyUp += enterApply;
                    animeDisplay.EpisodeTextbox.KeyUp += enterApply;

                    animeDisplay.NameTextbox.Text = item.Element("name").Value;
                    animeDisplay.EpisodeTextbox.Text = item.Element("episode").Value;
                    animeDisplay.ResolutionCombobox.Text = item.Element("resolution").Value;
                    animeDisplay.StatusCombobox.Text = item.Element("status").Value;
                    animeDisplay.AiringCheckbox.IsChecked = bool.Parse(item.Element("airing").Value);
                    animeDisplay.NameStrictCheckbox.IsChecked = bool.Parse(item.Element("name-strict").Value);

                    _settings.Subgroups.ToList().ForEach(s => animeDisplay.SubgroupComboBox.Items.Add(s));
                    animeDisplay.SubgroupComboBox.Text = subgroup.Equals("") ? "(None)" : subgroup;

                    _currentlyEditedAnime = item.Element("name").Value;
                }
            }
        }

        /// <summary>
        ///     The submission button event for the edit anime view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_anime_edit_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = _currentDisplay as Add;

            if (animeDisplay != null) {

                if (animeDisplay.NameTextbox.Text.Equals("") || animeDisplay.EpisodeTextbox.Text.Equals(""))
                    MessageBox.Show("There needs to be a name and/or episode.");

                else {
                    var subgroup = animeDisplay.SubgroupComboBox.Text;
                    var editedAnime = new Anime {
                        Name = animeDisplay.NameTextbox.Text,
                        Episode = $"{int.Parse(animeDisplay.EpisodeTextbox.Text):D2}",
                        Status = animeDisplay.StatusCombobox.Text,
                        Resolution = animeDisplay.ResolutionCombobox.Text,
                        Airing = animeDisplay.AiringCheckbox.IsChecked ?? false,
                        NameStrict = animeDisplay.NameStrictCheckbox.IsChecked ?? false,
                        PreferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup
                    };

                    _xml.EditAnime(_currentlyEditedAnime, editedAnime);
                    RefreshAnime();
                    pressButton(ButtonList);
                }
            }
        }

        /*
        /// <summary>
        ///     A test event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Click(object sender, RoutedEventArgs e) {
            SetAnimeEpisodeTotalToLastKnown();
            RefreshAnime();
        }
        */
    }
}