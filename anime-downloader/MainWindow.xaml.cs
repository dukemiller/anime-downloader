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
using anime_downloader.Classes;
using anime_downloader.Classes.Xml;
using anime_downloader.Views;
using Add = anime_downloader.Views.Add;
using Settings = anime_downloader.Classes.Settings;

namespace anime_downloader {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        /// <summary>
        ///     A collection of all the anime.
        /// </summary>
        private IEnumerable<Anime> _allAnime;

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

        private Xml _xml;

        public MainWindow() {
            InitializeComponent();
            ChangeDisplay(new Home());
            InitializeSettings();
        }

        /// <summary>
        ///     Initialize and set the settings object.
        /// </summary>
        private void InitializeSettings() {
            _settings = new Settings();
            _playlist = new Playlist(_settings);
            _xml = new Xml(_settings);
            
            if (!Directory.Exists(_settings.ApplicationPath))
                Directory.CreateDirectory(_settings.ApplicationPath);

            if (!File.Exists(_settings.SettingsXmlPath))
                CreateNewSettings();

            else {
                _xml.Verify.SettingsSchema();
                _xml.Verify.AnimeSchema();
            }

            if (!File.Exists(_settings.AnimeXmlPath))
                _xml.Create.AnimeXml();

            _allAnime = _xml.Controller.Animes.SortedWith(_settings.SortBy);
        }

        // Helper functions

        /// <summary>
        ///     Attempt to download anime and display the results.
        /// </summary>
        /// <param name="textbox">The output box to display results to.</param>
        /// <param name="animes">The collection of anime to try and get new episodes from.</param>
        private async void DownloadAnime(TextBox textbox, IEnumerable<Anime> animes) {
            var client = new WebClient();
            var downloaded = 0;

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
                        if (!nyaa.HasSubgroup())
                            continue;

                        // Nyaa listing with wrong subgroup
                        if (!_settings.Subgroups.Contains(nyaa.Subgroup()))
                            continue;
                    }

                    textbox.AppendText($"Downloading '{anime.Title}' episode '{anime.NextEpisode()}'.\n");
                    textbox.ScrollDown();
                    var filepath = Path.Combine(_settings?.TorrentFilesPath, nyaa.TorrentName());

                    if (!File.Exists(filepath))
                        await client.DownloadFileTaskAsync(nyaa.Link, filepath);

                    var command = $"/DIRECTORY \"{GetOutputFolder()}\" \"{filepath}\"";
                    CallCommand(_settings.UtorrentPath, command);

                    anime.Episode = anime.NextEpisode();
                    downloaded++;
                    break;
                }
            }
            
            textbox.AppendText(downloaded > 0
                ? $">> Found {downloaded} anime downloads."
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
            _currentDisplay = userDisplay;
            Display.Children.Clear();
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

            foreach (var finishedAnime in finishedAnimes) {
                var anime = _allAnime.FirstOrDefault(a => a.Name.ToLower().Contains(finishedAnime.Key.ToLower()));
                if (anime != null)
                    anime.Episode = $"{finishedAnime.Value:D2}";
            }
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

            animeListDisplay.DataGrid.Refresh(_xml.Controller.Animes.SortedWith(_settings.SortBy));

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

            var anime = selected.Item as Anime;
            anime?.Remove();
            animeListDisplay.DataGrid.Refresh(_xml.Controller.Animes.SortedWith(_settings.SortBy));
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
                var selected = animeListDisplay.DataGrid.SelectedCells.FirstOrDefault();
                if (!selected.IsValid)
                    return;

                var anime = selected.Item as Anime;
                anime?.Remove();
                animeListDisplay.DataGrid.Refresh(_xml.Controller.Animes.SortedWith(_settings.SortBy));
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
                Width = 400,
                Height = 30,
                Margin = new Thickness(470, 290, 0, 0),
                FontSize = 18
            };
            
            // Reset values and remove the find
            RoutedEventHandler closeFindWindow = (sender, routedEventArgs) => {
                Grid.Children.Remove(findWindow);
                animeListDisplay.DataGrid.ItemsSource = _allAnime;
                animeListDisplay.DataGrid.Focus();
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
                var copy = _allAnime.Where(a => a.Name.Contains(text));
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
            var settingsDisplay = ChangeDisplay(new Views.Settings()) as Views.Settings;
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
            var settingsDisplay = _currentDisplay as Views.Settings;
            if (settingsDisplay == null)
                return;

            if (settingsDisplay.BaseTextbox.Empty() || settingsDisplay.TorrentTextbox.Empty() ||
                settingsDisplay.DownloadTextbox.Empty())
                MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

            else {
                _settings.Subgroups = settingsDisplay.SubgroupsTextbox.Text.Split(new[] { ", " }, StringSplitOptions.None);
                _settings.BaseFolderPath = settingsDisplay.BaseTextbox.Text;
                _settings.UtorrentPath = settingsDisplay.DownloadTextbox.Text;
                _settings.TorrentFilesPath = settingsDisplay.TorrentTextbox.Text;
                _settings.OnlyWhitelisted = settingsDisplay.OnlyWhitelistedCheckbox.IsChecked ?? false;
            }
        }

        /// <summary>
        ///     The view to create a new settings profile.
        /// </summary>
        private void CreateNewSettings() {
            var settingsDisplay = ChangeDisplay(new Views.Settings()) as Views.Settings;
            if (settingsDisplay == null)
                return;

            this.ToggleButtons();
            settingsDisplay.ApplyChangesButton.Toggle();

            // Default guessed values
            settingsDisplay.BaseTextbox.Text = Directory.GetCurrentDirectory();
            settingsDisplay.TorrentTextbox.Text = Path.Combine(settingsDisplay.BaseTextbox.Text, "torrents");
            settingsDisplay.DownloadTextbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
            settingsDisplay.ApplyChangesButton.Content = "Create Profile";

            settingsDisplay.ApplyChangesButton.Click += (obj, ev) => {
                if (settingsDisplay.BaseTextbox.Empty() || settingsDisplay.TorrentTextbox.Empty() || settingsDisplay.DownloadTextbox.Empty())
                    MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");
                else {
                    _xml.Create.SettingsXml();
                    _settings.BaseFolderPath = settingsDisplay.BaseTextbox.Text;
                    _settings.TorrentFilesPath = settingsDisplay.TorrentTextbox.Text;
                    _settings.UtorrentPath = settingsDisplay.DownloadTextbox.Text;
                    _settings.Subgroups =
                        settingsDisplay.SubgroupsTextbox.Text.Split(new[] { " " },
                            StringSplitOptions.RemoveEmptyEntries);
                    _settings.OnlyWhitelisted = settingsDisplay.OnlyWhitelistedCheckbox.IsChecked ?? false;
                    _settings.SortBy = "name";

                    this.ToggleButtons();
                    ButtonHome.Press();
                }
            };
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
                if (downloadDisplay == null)
                    return;
                
                this.ToggleButtons();

                var online = await Nyaa.IsOnline();
                if (!online) {
                    downloadDisplay.TextBox.Text = ">> Nyaa is currently offline. Try checking later.";
                    this.ToggleButtons();
                }

                else {
                    var animes = _xml.Controller.Animes.Where(a => a.Airing && a.Status == "Watching");
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
            var animeDisplay = ChangeDisplay(new Views.Add()) as Views.Add;

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
            var animeDisplay = _currentDisplay as Views.Add;

            if (animeDisplay == null)
                return;

            if (animeDisplay.NameTextbox.Empty() || animeDisplay.EpisodeTextbox.Empty()) {
                MessageBox.Show("There needs to be a name and/or episode.");
            }

            else {
                var subgroup = animeDisplay.SubgroupComboBox.Text;
                if (subgroup.Equals("(None)"))
                    subgroup = "";

                var anime = new Anime {
                    Name = animeDisplay.NameTextbox.Text,
                    Episode = $"{int.Parse(animeDisplay.EpisodeTextbox.Text):D2}",
                    Status = animeDisplay.StatusCombobox.Text,
                    Resolution = animeDisplay.ResolutionCombobox.Text,
                    Airing = animeDisplay.AiringCheckbox.IsChecked ?? false,
                    NameStrict = animeDisplay.NameStrictCheckbox.IsChecked ?? false,
                    PreferredSubgroup = subgroup
                };

                _xml.Controller.Add(anime);
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
            var item = tableDisplay?.DataGrid.SelectedCells.FirstOrDefault();
            var anime = item?.Item as Anime;
            if (anime == null)
                return;

            var animeDisplay = ChangeDisplay(new Views.Add()) as Views.Add;
            if (animeDisplay == null)
                return;

            animeDisplay.AddButton.Content = "Edit";
            animeDisplay.AddButton.Click += ButtonAnimeEdit_Click;

            // Press enter to add the anime
            KeyEventHandler enterApply = (obj, k) => {
                if (k.Key != Key.Enter)
                    return;
                animeDisplay.AddButton.Focus();
                animeDisplay.AddButton.Press();
            };

            // Press Escape to go back
            KeyDown += (o, keyEventArgs) => {
                var key = keyEventArgs.Key;
                if (key == Key.Escape || key == Key.BrowserBack)
                    ButtonList.Press();
            };

            // Press mouse button back to go back
            MouseDown += (o, buttonEventArgs) => {
                if (buttonEventArgs.ChangedButton.Equals(MouseButton.XButton1))
                    ButtonList.Press();
            };

            animeDisplay.NameTextbox.KeyUp += enterApply;
            animeDisplay.EpisodeTextbox.KeyUp += enterApply;

            animeDisplay.NameTextbox.Text = anime.Name;
            animeDisplay.EpisodeTextbox.Text = anime.Episode;
            animeDisplay.ResolutionCombobox.Text = anime.Resolution;
            animeDisplay.StatusCombobox.Text = anime.Status;
            animeDisplay.AiringCheckbox.IsChecked = anime.Airing;
            animeDisplay.NameStrictCheckbox.IsChecked = anime.NameStrict;

            _settings.Subgroups.ToList().ForEach(s => animeDisplay.SubgroupComboBox.Items.Add(s));
            var subgroup = anime.PreferredSubgroup;
            animeDisplay.SubgroupComboBox.Text = subgroup != null && subgroup.Equals("") ? "(None)" : subgroup;

            _currentlyEditedAnime = anime.Name;
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

                var anime = _allAnime.Find(_currentlyEditedAnime);
                anime.Name = animeDisplay.NameTextbox.Text;
                anime.Episode = $"{int.Parse(animeDisplay.EpisodeTextbox.Text):D2}";
                anime.Status = animeDisplay.StatusCombobox.Text;
                anime.Resolution = animeDisplay.ResolutionCombobox.Text;
                anime.Airing = animeDisplay.AiringCheckbox.IsChecked ?? false;
                anime.NameStrict = animeDisplay.NameStrictCheckbox.IsChecked ?? false;
                anime.PreferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup;
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