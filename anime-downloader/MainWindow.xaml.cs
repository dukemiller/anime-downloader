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
        private IEnumerable<XElement> _allAnime;

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

            _settings.ApplicationPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "anime-downloader");

            if (!Directory.Exists(_settings.ApplicationPath))
                Directory.CreateDirectory(_settings.ApplicationPath);

            if (!File.Exists(_settings.SettingsXmlPath))
                NewSettings();

            if (!File.Exists(_settings.AnimeXmlPath))
                _xml.CreateAnimeXml();

            Xml.VerifySettingsXmlSchema(_settings.SettingsXmlPath);
            Xml.VerifyAnimeXmlSchema(_settings.AnimeXmlPath);

            var xmlSettings = XDocument.Load(_settings.SettingsXmlPath).Root;

            if (xmlSettings == null)
                return;

            _settings.BaseFolderPath = xmlSettings.Element("path")?.Element("base")?.Value;
            _settings.TorrentFilesPath = xmlSettings.Element("path")?.Element("torrents")?.Value;
            _settings.Subgroups = xmlSettings.Elements("subgroup").Elements("name").Select(x => x.Value).ToArray();
            _settings.UtorrentPath = xmlSettings.Element("path")?.Element("utorrent")?.Value;
            _settings.OnlyWhitelisted =
                bool.Parse(xmlSettings.Element("flag")?.Element("only-whitelisted-subs")?.Value ?? "false");
            _settings.SortBy = xmlSettings.Element("sortBy")?.Value;
        }

        /// <summary>
        ///     Update values in the anime variable.
        /// </summary>
        private void RefreshAnime() {
            _allAnime = XDocument.Load(_settings.AnimeXmlPath).Root?.Elements()
                .OrderBy(e => e.Element(_settings.SortBy)?.Value);
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
        private async void DownloadAnime(TextBox textbox, IEnumerable<Anime> animes) {
            var changedAnime = new List<Anime>();
            var client = new WebClient();
            var totalDownloaded = 0;

            textbox.Text = ">> Searching for currently airing anime episodes ...\n";

            foreach (var anime in animes) {
                var nyaaLinks = await anime.GetLinksToNextEpisode();

                if (nyaaLinks == null)
                    continue;

                foreach (var nyaa in nyaaLinks) {
                    if (nyaa == null)
                        continue;

                    // Most likely wrong torrent
                    if (anime.NameStrict && !anime.Name.Equals(nyaa.StrippedName(true)))
                        continue;

                    // Not the right subgroup
                    if (!anime.PreferredSubgroup.Equals("") & !nyaa.Subgroup().Contains(anime.PreferredSubgroup))
                        continue;

                    if (_settings.OnlyWhitelisted) {
                        // Nyaa listing with no subgroup in the title
                        if (!nyaa.HasSubgroup()) {
                            continue;
                        }

                        // Nyaa listing with wrong subgroup
                        if (!_settings.Subgroups.Contains(nyaa.Subgroup())) {
                            continue;
                        }
                    }

                    textbox.AppendText($"Downloading '{anime.Title()}' episode '{anime.NextEpisode()}'.\n");
                    textbox.ScrollDown();
                    var filepath = Path.Combine(_settings?.TorrentFilesPath, nyaa.TorrentName());

                    if (!File.Exists(filepath))
                        await client.DownloadFileTaskAsync(nyaa.Link, filepath);

                    var command = $"/DIRECTORY \"{GetOutputFolder()}\" \"{filepath}\"";
                    CallCommand(_settings.UtorrentPath, command);

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
            textbox.ScrollDown();
            this.ToggleButtons();
        }
        
        /// <summary>
        ///     Execute new process with given parameters.
        /// </summary>
        /// <param name="executable">Path to the executable file.</param>
        /// <param name="parameters">Arguments given to the executable.</param>
        private static void CallCommand(string executable, string parameters) {
            var info = new ProcessStartInfo {
                FileName = executable,
                Arguments = parameters,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = new Process {
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
        ///     Change the current main display to another display.
        /// </summary>
        /// <example>var tableDisplay = ChangeDisplay(new TableView()) as TableView());</example>
        /// <param name="userDisplay">The user supplied display.</param>
        private UserControl ChangeDisplay(UserControl userDisplay) {
            if (_currentDisplay != null && _currentDisplay.GetType() == userDisplay.GetType())
                return _currentDisplay;
            Display.Children.Clear();
            _currentDisplay = userDisplay;
            Display.Children.Add(_currentDisplay);
            return _currentDisplay;
        }
        
        /// <summary>
        ///     Strip the video name of all tags (resolution, seeders, etc).
        /// </summary>
        /// <param name="name">A downloaded file's name.</param>
        /// <returns></returns>
        private static string StripFilename(string name) {
            const string pattern = @"(\[(?:.*?)\])|(\((?:.*)\))";

            return (from Match match in Regex.Matches(name, pattern)
                    where match.Groups.Count > 1
                    select match.Groups[1].Length > 0 ? match.Groups[1].Value : match.Groups[2].Value into m
                    where m.Length > 0
                    select m).Aggregate(name, (current, m) => current.Replace(m, "").Trim());
        }

        /// <summary>
        ///     Returns a collection of [{animeName: lastGivenEpisode}] from a list of stripped titles.
        /// </summary>
        /// <param name="strippedNames">A collection of names passed through stripFilename().</param>
        /// <returns></returns>
        private static Dictionary<string, int> CollectLastEpisode(IEnumerable<string> strippedNames) {
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

            var finishedAnimes = CollectLastEpisode(Directory.GetFiles(path)
                .Select(Path.GetFileName)
                .Select(StripFilename)
                .ToArray());

            // I'm not sure how to cleanly turn this into a generic XML function above.
            var document = XDocument.Load(_settings.AnimeXmlPath);
            var root = document.Root;

            if (root == null)
                return;

            Func<string, string, bool> contains = (e, a) =>
                e.ToLower().Contains(a.ToLower());
            
            foreach (var entry in finishedAnimes) {
                if (entry.Key == null)
                    continue;
                
                var selected = root.Elements()
                    .Where(anime => {
                        var xElement = anime.Element("name");
                        return xElement != null && contains(entry.Key, xElement.Value);
                    })
                    .FirstOrDefault();

                selected?.SetElementValue("episode", $"{entry.Value:D2}");
            }

            document.Save(_settings.AnimeXmlPath);
        }
        
        // Event handling

        /// <summary>
        ///     The home view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonHome_Click(object sender, RoutedEventArgs e) {
            ChangeDisplay(new Home());
        }

        /// <summary>
        ///     The button to open your base folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonFolder_Click(object sender, RoutedEventArgs e) {
            if (Directory.Exists(_settings.BaseFolderPath))
                Process.Start(_settings.BaseFolderPath);
            else
                MessageBox.Show("Your base folder doesn't seem to exist.");
        }

        /// <summary>
        ///     The button to open your settings folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOpenExecuting_Click(object sender, RoutedEventArgs e) {
            Process.Start(_settings.ApplicationPath);
        }

        /// <summary>
        ///     The playlist creator view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPlaylist_Click(object sender, RoutedEventArgs e) {
            var playlistDisplay = ChangeDisplay(new PlaylistCreator()) as PlaylistCreator;
            if (playlistDisplay == null)
                return;

            playlistDisplay.CreateButton.Click += PlaylistCreateButton_Click;
        }

        /// <summary>
        ///     The button submission event for creating a playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistCreateButton_Click(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(_settings.BaseFolderPath))
                MessageBox.Show("Your base folder doesn't seem to exist.");

            else {
                _playlist.Refresh();

                var playlistCreatorDisplay = _currentDisplay as PlaylistCreator;

                if (playlistCreatorDisplay == null)
                    return;

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

        /// <summary>
        ///     The view to display the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonList_Click(object sender, RoutedEventArgs e) {
            var animeListDisplay = ChangeDisplay(new AnimeList()) as AnimeList;
            if (animeListDisplay == null)
                return;

            animeListDisplay.Add.Click += ButtonAddNew_Click;
            animeListDisplay.Edit.Click += AnimeListEdit_Click;
            animeListDisplay.Delete.Click += AnimeListDelete_Click;
            animeListDisplay.DataGrid.PreviewKeyDown += AnimeListDelete_KeyDown;
            animeListDisplay.DataGrid.MouseDoubleClick += AnimeList_MouseDoubleClick;
            animeListDisplay.DataGrid.ItemsSource = _allAnime;

            Grid.KeyDown += (o, keyEventArgs) => {
                if (keyEventArgs.Key == Key.F && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) {
                    AnimeListFind();
                }
            };
        }

        /// <summary>
        ///     The context menu event for the anime list view, selection: "Delete"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListDelete_Click(object sender, RoutedEventArgs e) {
            var animeListDisplay = _currentDisplay as AnimeList;
            if (animeListDisplay == null)
                return;

            var selected = animeListDisplay.DataGrid.SelectedCells.FirstOrDefault();
            if (!selected.IsValid)
                return;

            var row = selected.Item as XElement;
            if (row == null)
                return;

            _xml.RemoveAnime(row.Element("name")?.Value);
            RefreshAnime();
        }

        /// <summary>
        ///     The keydown event for the anime list view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListDelete_KeyDown(object sender, KeyEventArgs e) {
            var animeListDisplay = _currentDisplay as AnimeList;

            if (animeListDisplay == null)
                return;

            if (e.Key == Key.Delete) {
                var row = animeListDisplay.DataGrid?.SelectedCells?[0].Item as XElement;
                _xml.RemoveAnime(row?.Element("name")?.Value);
                RefreshAnime();
            }
        }

        private void AnimeListFind() {
            var animeListDisplay = _currentDisplay as AnimeList;
            if (animeListDisplay == null)
                return;

            // Don't recreate it again
            var box = Grid.Children.OfType<TextBox>().FirstOrDefault(t => t.Name.Equals("FindBox"));
            if (box != null)
                return;

            var findWindow = new TextBox {
                Name = "FindBox",
                Width = 400, Height = 30,
                Margin = new Thickness(470, 290, 0, 0),
                FontSize = 18
            };
            
            // Reset values and remove the find
            RoutedEventHandler closeFindWindow = (sender, routedEventArgs) => {
                Grid.Children.Remove(findWindow);
                animeListDisplay.DataGrid.ItemsSource = _allAnime;
                animeListDisplay.DataGrid.Focus();
                // Keyboard.ClearFocus();
            };

            MouseButtonEventHandler closeFindWindowMouse = (sender, mouseButtonEventArgs) => {
                closeFindWindow(sender, mouseButtonEventArgs);
            };

            // --> Closing the find
            // Make any button press close the find window, and going into anime details too
            this.GetAll<Button>().ForEach(b => b.Click += closeFindWindow);
            animeListDisplay.DataGrid.MouseDoubleClick += closeFindWindowMouse;

            // CTRL-F again or Escape also close find
            Grid.KeyDown += (sender, keyEventArgs) => {
                if (keyEventArgs.Key == Key.Escape)
                    closeFindWindow(sender, keyEventArgs);
                else if (keyEventArgs.Key == Key.F && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) {
                    if (findWindow.IsSelectionActive)
                        closeFindWindow(sender, keyEventArgs);
                    else
                        findWindow.Focus();
                }
            };

            // --> The actual functionality
            findWindow.KeyUp += (sender, keyEventArgs) => {
                var text = findWindow.Text.ToLower();
                var copy = _allAnime.Where(a => a.Element("name")?.Value.ToLower().Contains(text) ?? true);
                animeListDisplay.DataGrid.ItemsSource = copy;
            };
            
            Grid.Children.Add(findWindow);
            findWindow.Focus();
        }
        
        /// <summary>
        ///     The double click event for the anime list view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeList_MouseDoubleClick(object sender, MouseEventArgs e) {
            var animeListDisplay = _currentDisplay as AnimeList;

            if (animeListDisplay == null)
                return;

            var selected = animeListDisplay.DataGrid.SelectedCells.FirstOrDefault();

            if (selected.IsValid) {
                AnimeListEdit_Click(sender, e);
            }
        }

        /// <summary>
        ///     The view to manage settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSettings_Click(object sender, RoutedEventArgs e) {
            if (_settings == null)
                return;

            var settingsDisplay = ChangeDisplay(new UserControls.Settings()) as UserControls.Settings;

            if (settingsDisplay == null)
                return;

            settingsDisplay.BaseTextbox.Text = _settings.BaseFolderPath;
            settingsDisplay.SubgroupsTextbox.Text = string.Join(", ", _settings.Subgroups);
            settingsDisplay.DownloadTextbox.Text = _settings.UtorrentPath;
            settingsDisplay.TorrentTextbox.Text = _settings.TorrentFilesPath;
            settingsDisplay.ApplyChangesButton.Click += ButtonApplySettings_Click;
            settingsDisplay.OnlyWhitelistedCheckbox.IsChecked = _settings.OnlyWhitelisted;
        }

        /// <summary>
        ///     The submission button event for the edit settings view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonApplySettings_Click(object sender, RoutedEventArgs e) {
            var settingsDisplay = _currentDisplay as UserControls.Settings;

            if (settingsDisplay == null)
                return;

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
                            new XElement("name", Environment.UserName),
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

        /// <summary>
        ///     The view to create a new settings profile.
        /// </summary>
        private void NewSettings() {
            if (_settings == null)
                return;

            var settingsDisplay = ChangeDisplay(new UserControls.Settings()) as UserControls.Settings;

            if (settingsDisplay == null)
                return;

            this.ToggleButtons();
            settingsDisplay.BaseTextbox.Text = Directory.GetCurrentDirectory();
            settingsDisplay.TorrentTextbox.Text = Path.Combine(settingsDisplay.BaseTextbox.Text, "torrents");
            settingsDisplay.DownloadTextbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
            settingsDisplay.ApplyChangesButton.Content = "Create Profile";

            settingsDisplay.ApplyChangesButton.Click += (obj, ev) => {
                if (settingsDisplay.BaseTextbox.Empty() || settingsDisplay.TorrentTextbox.Empty() || settingsDisplay.DownloadTextbox.Empty())
                    MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");
                else {
                    CreateSettings(settingsDisplay);
                }
            };
        }

        private void CreateSettings(UserControls.Settings settingsDisplay) {
            _settings.BaseFolderPath = settingsDisplay.BaseTextbox.Text;
            _settings.TorrentFilesPath = settingsDisplay.TorrentTextbox.Text;
            _settings.UtorrentPath = settingsDisplay.DownloadTextbox.Text;
            _settings.Subgroups =
                settingsDisplay.SubgroupsTextbox.Text.Split(new[] {" "},
                    StringSplitOptions.RemoveEmptyEntries);
            _settings.OnlyWhitelisted = settingsDisplay.OnlyWhitelistedCheckbox.IsChecked ?? false;
            _settings.SortBy = "name";
            _xml.CreateSettingsXml(_settings);

            this.ToggleButtons();
            InitializeSettings();
            ButtonHome.Press();
        }

        /// <summary>
        ///     The view to check for new anime and download it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonCheck_Click(object sender, RoutedEventArgs e) {
            if (!Directory.Exists(_settings.BaseFolderPath))
                MessageBox.Show("Your base folder doesn't seem to exist.");

            else if (!File.Exists(_settings.UtorrentPath) || !_settings.UtorrentPath.ToLower().EndsWith(".exe"))
                MessageBox.Show("Your uTorrent.exe path seems to be wrong.");

            else {
                if (!Directory.Exists(_settings.TorrentFilesPath))
                    Directory.CreateDirectory(_settings.TorrentFilesPath);

                var downloadDisplay = ChangeDisplay(new Download()) as Download;

                var animeXml = XDocument.Load(_settings.AnimeXmlPath);

                var animes = animeXml.Element("anime")?.Elements()
                    .Select(x => new Anime(x))
                    .Where(a => a.Airing && a.Status == "Watching")
                    .ToArray();

                this.ToggleButtons();

                var online = await Nyaa.IsOnline();

                if (downloadDisplay == null)
                    return;

                if (!online) {
                    downloadDisplay.TextBox.Text = ">> Nyaa is currently offline. Try checking later.";
                    this.ToggleButtons();
                }

                else {
                    DownloadAnime(downloadDisplay.TextBox, animes);
                }
            }
        }

        /// <summary>
        ///     The view to add new anime to the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAddNew_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = ChangeDisplay(new Add()) as Add;

            if (animeDisplay == null)
                return;

            animeDisplay.AddButton.Click += ButtonAdd_Click;

            KeyEventHandler enterApply = (obj, k) => {
                if (k.Key != Key.Enter)
                    return;

                animeDisplay.AddButton.Focus();
                animeDisplay.AddButton.Press();
            };

            animeDisplay.NameTextbox.KeyUp += enterApply;
            animeDisplay.EpisodeTextbox.KeyUp += enterApply;
            _settings.Subgroups.ToList().ForEach(s => animeDisplay.SubgroupComboBox.Items.Add(s));
        }

        /// <summary>
        ///     The submission button event for the add anime view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAdd_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = _currentDisplay as Add;

            if (animeDisplay == null)
                return;

            if (animeDisplay.NameTextbox.Empty() || animeDisplay.EpisodeTextbox.Empty()) {
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
                ButtonList.Press();
            }
        }

        /// <summary>
        ///     The view to edit anime selected from the anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListEdit_Click(object sender, RoutedEventArgs e) {
            var tableDisplay = _currentDisplay as AnimeList;

            if (tableDisplay == null)
                return;

            var item = tableDisplay.DataGrid.SelectedCells[0].Item as XElement; //  .Items[0].ToString());

            var animeDisplay = ChangeDisplay(new Add()) as Add;
            if (animeDisplay == null)
                return;

            animeDisplay.AddButton.Content = "Edit";
            animeDisplay.AddButton.Click += ButtonAnimeEdit_Click;

            KeyEventHandler enterApply = (obj, k) => {
                if (k.Key != Key.Enter)
                    return;
                animeDisplay.AddButton.Focus();
                animeDisplay.AddButton.Press();
            };

            if (item == null)
                return;

            var subgroup = item.Element("preferredSubgroup")?.Value;

            animeDisplay.NameTextbox.KeyUp += enterApply;
            animeDisplay.EpisodeTextbox.KeyUp += enterApply;

            animeDisplay.NameTextbox.Text = item.Element("name")?.Value;
            animeDisplay.EpisodeTextbox.Text = item.Element("episode")?.Value;
            animeDisplay.ResolutionCombobox.Text = item.Element("resolution")?.Value;
            animeDisplay.StatusCombobox.Text = item.Element("status")?.Value;
            animeDisplay.AiringCheckbox.IsChecked = bool.Parse(item.Element("airing")?.Value ?? "false");
            animeDisplay.NameStrictCheckbox.IsChecked = bool.Parse(item.Element("name-strict")?.Value ?? "false");

            _settings.Subgroups.ToList().ForEach(s => animeDisplay.SubgroupComboBox.Items.Add(s));
            animeDisplay.SubgroupComboBox.Text = subgroup != null && subgroup.Equals("") ? "(None)" : subgroup;

            _currentlyEditedAnime = item.Element("name")?.Value;
        }

        /// <summary>
        ///     The submission button event for the edit anime view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAnimeEdit_Click(object sender, RoutedEventArgs e) {
            var animeDisplay = _currentDisplay as Add;

            if (animeDisplay == null)
                return;

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
                ButtonList.Press();
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