using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Classes;
using anime_downloader.Classes.Xml;
using anime_downloader.Views;
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
        ///     Handle playlist creation with some customization.
        /// </summary>
        private Playlist _playlist;

        /// <summary>
        ///     Handles paths and user settings.
        /// </summary>
        private Settings _settings;

        /// <summary>
        ///     Handles objects for modifying and creating the xml files
        /// </summary>
        private Xml _xml;

        /// <summary>
        ///     Handles file logging operations.
        /// </summary>
        private Logger _logger;

        /// <summary>
        ///     Handles downloading operations.
        /// </summary>
        private Downloader _downloader;

        public MainWindow() {
            InitializeComponent();
            InitializeSettings();
        }

        /// <summary>
        ///     Initialize and set the settings object.
        /// </summary>
        private void InitializeSettings() {
            _settings = new Settings();
            _playlist = new Playlist(_settings);
            _xml = new Xml(_settings);
            _logger = new Logger(_settings);
            _downloader = new Downloader(_settings);
            
            if (!Directory.Exists(_settings.ApplicationPath))
                Directory.CreateDirectory(_settings.ApplicationPath);

            // Create new settings xml or edit the schema and load anime
            if (!File.Exists(_settings.SettingsXmlPath))
                CreateNewSettings();

            else {
                _xml.Verify.SettingsSchema();
                _xml.Verify.AnimeSchema();
                _allAnime = _xml.Controller.SortedAnimes;
                ChangeDisplay<Home>();
            }

            // Create new anime xml
            if (!File.Exists(_settings.AnimeXmlPath))
                _xml.Create.AnimeXmlAndSave();
        }

        // Helper functions

        /// <summary>
        ///     Change the display to UserControl TView.
        /// </summary>
        /// <remarks>
        ///     Only use this in view changing methods, don't use this to
        ///     get a variable as the current views type for modifying
        ///     it's elements.
        /// </remarks>
        /// <typeparam name="TView">A name of a class in the Views folders</typeparam>
        /// <returns>
        ///     A an instantiated view of type TView
        /// </returns>
        private TView ChangeDisplay<TView>() where TView : UserControl, new() {
            // Don't reload the same view
            if (_currentDisplay != null && _currentDisplay.GetType() == typeof(TView))
                return (TView) _currentDisplay;
            _currentDisplay = new TView(); 
            Display.Children.Clear();
            Display.Children.Add(_currentDisplay);
            return (TView) _currentDisplay;
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
        ///     View: Home.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonHome_Click(object sender, RoutedEventArgs e) {
            ChangeDisplay<Home>();
        }

        /// <summary>
        ///     Event: Open base folder
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
        ///     Event: Open settings folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOpenExecuting_Click(object sender, RoutedEventArgs e) {
            Process.Start(_settings.ApplicationPath);
        }

        // 

        /// <summary>
        ///     View: Playlist Creator.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPlaylist_Click(object sender, RoutedEventArgs e) {
            var playlistDisplay = ChangeDisplay<PlaylistCreator>();
            if (playlistDisplay == null)
                return;

            playlistDisplay.CreateButton.Click += PlaylistCreateButton_Click;
        }

        /// <summary>
        ///     Event: Submit -> Playlist
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

        // 

        /// <summary>
        ///     View: Anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonList_Click(object sender, RoutedEventArgs e) {
            var display = ChangeDisplay<AnimeList>();

            display.DataGrid.Refresh(_xml.Controller.SortedAnimes);

            display.Add.Click += ButtonAddNew_Click;
            display.Edit.Click += AnimeListEdit_Click;
            display.Delete.Click += AnimeListDelete_Click;
            display.DataGrid.PreviewKeyDown += AnimeListDelete_KeyDown;
            display.DataGrid.MouseDoubleClick += AnimeList_MouseDoubleClick;
            display.DataGrid.ItemsSource = _allAnime;

            Grid.KeyDown += (o, keyEventArgs) => {
                if (keyEventArgs.Key == Key.F && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) {
                    CreateAnimeFindPopup();
                }
            };
        }

        /// <summary>
        ///     Event: Submit -> Anime list (delete)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListDelete_Click(object sender, RoutedEventArgs e) {
            var display = (AnimeList) _currentDisplay;
            var selected = display?.DataGrid.SelectedCells.FirstOrDefault();
            if (!selected?.IsValid ?? false)
                return;

            var anime = selected?.Item as Anime;
            anime?.Remove();
            display?.DataGrid.Refresh(_xml.Controller.SortedAnimes);
        }
        
        /// <summary>
        ///     Event: Keydown -> Anime list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListDelete_KeyDown(object sender, KeyEventArgs e) {
            var display = (AnimeList) _currentDisplay;

            // Delete
            if (e.Key == Key.Delete) {
                var selected = display.DataGrid.SelectedCells.FirstOrDefault();
                if (!selected.IsValid)
                    return;
                var anime = selected.Item as Anime;
                anime?.Remove();
                display.DataGrid.Refresh(_xml.Controller.SortedAnimes);
            }

            // Edit
            else if (e.Key == Key.Enter) {
                if (display.DataGrid.SelectedCells.FirstOrDefault().IsValid) {
                    AnimeListEdit_Click(sender, e);
                }
            }
        }

        /// <summary>
        ///     Secondary View: Find anime box
        /// </summary>
        private void CreateAnimeFindPopup() {
            var display = (AnimeList) _currentDisplay;

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
            RoutedEventHandler closeFindWindow = delegate {
                Grid.Children.Remove(findWindow);
                display.DataGrid.ItemsSource = _allAnime;
                display.DataGrid.Focus();
            };

            MouseButtonEventHandler closeFindWindowMouse = delegate {
                Grid.Children.Remove(findWindow);
                display.DataGrid.ItemsSource = _allAnime;
                display.DataGrid.Focus();
            };

            // --> Closing the find
            // Make any button press close the find window, and going into anime details too
            this.GetAll<Button>().ForEach(b => b.Click += closeFindWindow);
            display.DataGrid.MouseDoubleClick += closeFindWindowMouse;

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
            findWindow.KeyUp += delegate {
                var text = findWindow.Text.ToLower().Trim();
                var copy = _allAnime.Where(a => a.Name.ToLower().Contains(text));
                display.DataGrid.ItemsSource = copy;
            };

            Grid.Children.Add(findWindow);
            findWindow.Focus();
        }

        /// <summary>
        ///     Event: Double click -> Anime list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeList_MouseDoubleClick(object sender, MouseEventArgs e) {
            var display = (AnimeList) _currentDisplay;
            var selected = display.DataGrid.SelectedCells.FirstOrDefault();
            if (selected.IsValid) {
                AnimeListEdit_Click(sender, e);
            }
        }

        // 

        /// <summary>
        ///     View: AnimeDetails (add)    
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAddNew_Click(object sender, RoutedEventArgs e) {
            var display = ChangeDisplay<AnimeDetails>();
            display.AddButton.Click += ButtonAdd_Click;

            // Enter will create the anime
            KeyEventHandler enterToAdd = (obj, k) => {
                if (k.Key != Key.Enter)
                    return;

                display.AddButton.Focus();
                display.AddButton.Press();
            };

            // Focus the name textbox on load
            display.NameTextbox.Loaded += delegate {
                display.NameTextbox.Focus();
            };

            display.NameTextbox.KeyUp += enterToAdd;
            display.EpisodeTextbox.KeyUp += enterToAdd;
            _settings.Subgroups.ToList().ForEach(s => display.SubgroupComboBox.Items.Add(s));

        }

        /// <summary>
        ///     Event: Submit -> AnimeDetails (add)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAdd_Click(object sender, RoutedEventArgs e) {
            var display = (AnimeDetails) _currentDisplay;

            if (display.NameTextbox.Empty() ||
                display.EpisodeTextbox.Empty())
                MessageBox.Show("There needs to be a name and/or episode.");

            else {
                var subgroup = display.SubgroupComboBox.Text;
                if (subgroup.Equals("(None)"))
                    subgroup = "";

                var anime = new Anime {
                    Name = display.NameTextbox.Text,
                    Episode = $"{int.Parse(display.EpisodeTextbox.Text):D2}",
                    Status = display.StatusCombobox.Text,
                    Resolution = display.ResolutionCombobox.Text,
                    Airing = display.AiringCheckbox.IsChecked ?? false,
                    NameStrict = display.NameStrictCheckbox.IsChecked ?? false,
                    PreferredSubgroup = subgroup
                };

                _xml.Controller.Add(anime);
                ButtonList.Press();
            }
        }

        /// <summary>
        ///     View: AnimeDetails (edit)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListEdit_Click(object sender, RoutedEventArgs e) {
            var tableDisplay = (AnimeList) _currentDisplay;
            var anime = tableDisplay.DataGrid.SelectedCells.FirstOrDefault().Item as Anime;
            if (anime == null)
                return;

            var display = ChangeDisplay<AnimeDetails>();
            display.AddButton.Content = "Edit";
            display.AddButton.Click += ButtonAnimeEdit_Click;

            // Press enter to add the anime
            KeyEventHandler enterApply = (obj, k) => {
                if (k.Key != Key.Enter)
                    return;
                display.AddButton.Focus();
                display.AddButton.Press();
            };

            // Press Escape to go back
            KeyDown += (o, keyEventArgs) => {
                var key = keyEventArgs.Key;
                if (key == Key.Escape || key == Key.BrowserBack) {
                    ButtonHome.Press();
                    ButtonList.Press();
                }
            };

            // Press mouse button back to go back
            MouseDown += (o, buttonEventArgs) => {
                if (buttonEventArgs.ChangedButton.Equals(MouseButton.XButton1)) {
                    ButtonHome.Press();
                    ButtonList.Press();
                }
            };

            display.NameTextbox.KeyDown += enterApply;
            display.EpisodeTextbox.KeyDown += enterApply;

            display.NameTextbox.Text = anime.Name;
            display.EpisodeTextbox.Text = anime.Episode;
            display.ResolutionCombobox.Text = anime.Resolution;
            display.StatusCombobox.Text = anime.Status;
            display.AiringCheckbox.IsChecked = anime.Airing;
            display.NameStrictCheckbox.IsChecked = anime.NameStrict;

            _settings.Subgroups.ToList().ForEach(s => display.SubgroupComboBox.Items.Add(s));
            var subgroup = anime.PreferredSubgroup;
            display.SubgroupComboBox.Text = subgroup != null && subgroup.Equals("") ? "(None)" : subgroup;

            _currentlyEditedAnime = anime.Name;
        }

        /// <summary>
        ///     Event: Submit -> AnimeDetails (edit)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAnimeEdit_Click(object sender, RoutedEventArgs e) {
            var display = (AnimeDetails) _currentDisplay;

            if (display.NameTextbox.Empty() ||
                display.EpisodeTextbox.Empty())
                MessageBox.Show("There needs to be a name and/or episode.");

            else {
                var subgroup = display.SubgroupComboBox.Text;
                var anime = _allAnime.Get(_currentlyEditedAnime);
                anime.Name = display.NameTextbox.Text;
                anime.Episode = $"{int.Parse(display.EpisodeTextbox.Text):D2}";
                anime.Status = display.StatusCombobox.Text;
                anime.Resolution = display.ResolutionCombobox.Text;
                anime.Airing = display.AiringCheckbox.IsChecked ?? false;
                anime.NameStrict = display.NameStrictCheckbox.IsChecked ?? false;
                anime.PreferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup;
                ButtonList.Press();
            }
        }

        // 

        /// <summary>
        ///     View: Settings (edit)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSettings_Click(object sender, RoutedEventArgs e) {
            var display = ChangeDisplay<Views.Settings>();

            display.GetAll<TextBox>().ForEach(t => t.KeyUp += (o, k) => {
                if (k.Key == Key.Enter)
                    display.ApplyChangesButton.Press();
            });

            display.BaseTextbox.Text = _settings.BaseFolderPath;
            display.SubgroupsTextbox.Text = string.Join(", ", _settings.Subgroups);
            display.DownloadTextbox.Text = _settings.UtorrentPath;
            display.TorrentTextbox.Text = _settings.TorrentFilesPath;
            display.ApplyChangesButton.Click += ButtonApplySettings_Click;
            display.OnlyWhitelistedCheckbox.IsChecked = _settings.OnlyWhitelisted;
            display.UseLoggerCheckbox.IsChecked = _settings.UseLogging;
        }

        /// <summary>
        ///     Event: Submit -> Settings (edit)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonApplySettings_Click(object sender, RoutedEventArgs e) {
            var display = (Views.Settings) _currentDisplay;

            if (display.BaseTextbox.Empty() ||
                display.TorrentTextbox.Empty() ||
                display.DownloadTextbox.Empty())
                MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

            else {
                _settings.Subgroups = display.SubgroupsTextbox.Text.Split(new[] {", "},
                    StringSplitOptions.RemoveEmptyEntries);
                _settings.BaseFolderPath = display.BaseTextbox.Text;
                _settings.UtorrentPath = display.DownloadTextbox.Text;
                _settings.TorrentFilesPath = display.TorrentTextbox.Text;
                _settings.OnlyWhitelisted = display.OnlyWhitelistedCheckbox.IsChecked ?? false;
                _settings.UseLogging = display.UseLoggerCheckbox.IsChecked ?? false;
            }
        }

        /// <summary>
        ///     View: Settings (new)
        /// </summary>
        private void CreateNewSettings() {
            this.ToggleButtons();
            var display = ChangeDisplay<Views.Settings>();
            display.ApplyChangesButton.Toggle();

            // Default guessed values
            display.BaseTextbox.Text = Directory.GetCurrentDirectory();
            display.TorrentTextbox.Text = Path.Combine(display.BaseTextbox.Text, "torrents");
            display.DownloadTextbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
            display.ApplyChangesButton.Content = "Create Profile";

            display.ApplyChangesButton.Click += (obj, ev) => {

                if (display.BaseTextbox.Empty() ||
                    display.TorrentTextbox.Empty() ||
                    display.DownloadTextbox.Empty())
                    MessageBox.Show("You must enter in Base, Torrent or Utorrent Path Boxes.");

                else {
                    _xml.Create.SettingsXmlAndSave();
                    _settings.BaseFolderPath = display.BaseTextbox.Text;
                    _settings.TorrentFilesPath = display.TorrentTextbox.Text;
                    _settings.UtorrentPath = display.DownloadTextbox.Text;
                    _settings.Subgroups =
                        display.SubgroupsTextbox.Text.Split(new[] {" "},
                            StringSplitOptions.RemoveEmptyEntries);
                    _settings.OnlyWhitelisted = display.OnlyWhitelistedCheckbox.IsChecked ?? false;
                    _settings.UseLogging = display.UseLoggerCheckbox.IsChecked ?? false;
                    _settings.SortBy = "name";

                    _allAnime = _xml.Controller.SortedAnimes;
                    this.ToggleButtons();
                    ChangeDisplay<Home>();
                }
            };
        }

        // 

        /// <summary>
        ///     View: Download anime
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

                var downloadDisplay = ChangeDisplay<Download>();
                var textBox = downloadDisplay.TextBox;
                this.ToggleButtons();
                var online = await Nyaa.IsOnline();

                if (!online) {
                    textBox.Text = ">> Nyaa is currently offline. Try checking later.";
                    this.ToggleButtons();
                }

                else {
                    var downloaded = await _downloader.DownloadAnime(_xml.Controller.AiringAnimes, textBox, _logger);
                    textBox.AppendText(downloaded > 0
                        ? $">> Found {downloaded} anime downloads."
                        : ">> No new anime found.");
                    textBox.ScrollDown();
                    this.ToggleButtons();
                }
            }
        }

        /// <summary>
        ///     View: Misc
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonMisc_Click(object sender, RoutedEventArgs e) {
            ChangeDisplay<Misc>();
        }
    }
}