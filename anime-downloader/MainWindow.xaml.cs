using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Classes;
using anime_downloader.Classes.FileHandling;
using anime_downloader.Classes.Web;
using anime_downloader.Classes.Xml;
using anime_downloader.Views;
using Settings = anime_downloader.Classes.Settings;
using UserControl = System.Windows.Controls.UserControl;

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

        private FileHandler _filehandler;

        private System.Windows.Forms.NotifyIcon _notifyIcon;

        public MainWindow() {
            InitializeComponent();
            InitializeSettings();
            InitializeTray();
        }

        // Helper functions

        /// <summary>
        ///     Initialize and set the settings object.
        /// </summary>
        private void InitializeSettings() {
            _settings = new Settings();
            _playlist = new Playlist(_settings);
            _xml = new Xml(_settings);
            _logger = new Logger(_settings);
            _downloader = new Downloader(_settings);
            _filehandler = new FileHandler(_settings, _downloader, _logger);
            
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

        private void InitializeTray() {
            // get the image from the program
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("anime_downloader.ad3.ico");
            Debug.Assert(stream != null, "myStream != null");
            var icon = new Icon(stream);

            _notifyIcon = new System.Windows.Forms.NotifyIcon {
                Icon = icon
                // Icon = new System.Drawing.Icon(image)
            };

            _notifyIcon.Click += delegate {
                Show();
                WindowState = WindowState.Normal;
            };
        }
        
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

        private void Window_StateChanged(object sender, EventArgs e) {
            if (WindowState == WindowState.Minimized) {
                Hide();
                _notifyIcon.Visible = true;
            }
            else if (WindowState == WindowState.Normal) {
                _notifyIcon.Visible = false;
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
        ///     View -> Add Multiple
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListAddMultiple_Click(object sender, RoutedEventArgs e) {
            var display = ChangeDisplay<AnimeDetailsMultiple>();

            display.InputTextBox.Loaded += delegate {
                display.InputTextBox.Focus();
            };

            display.SubmitButton.Click += delegate {
                var names = display.InputTextBox.Text.Split(Environment.NewLine.ToCharArray(), 
                    StringSplitOptions.RemoveEmptyEntries).Select(n => n.ToLower()).ToList();
                if (names.Distinct().Count() != names.Count)
                    MessageBox.Show("Names have to be unique.");
                else if (_allAnime.Select(a => a.Name.ToLower()).Intersect(names).Any())
                    MessageBox.Show("A title entered already exists in the anime list.");
                else {
                    foreach (var name in names) {
                        _xml.Controller.Add(new Anime {
                            Name = name,
                            Airing = display.AiringCheckBox.IsChecked ?? false,
                            Episode = display.EpisodeTextBox.Text,
                            Status = display.StatusComboBox.Text,
                            Resolution = display.ResolutionComboBox.Text
                        });
                    }
                    ButtonList.Press();
                }
            };
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
            display.AddMultiple.Click += AnimeListAddMultiple_Click;
            display.DataGrid.PreviewKeyDown += AnimeListDelete_KeyDown;
            display.DataGrid.MouseDoubleClick += AnimeList_MouseDoubleClick;

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
            foreach (var cell in display.DataGrid.SelectedCells)
                _xml.Controller.Remove(cell.Item as Anime);
            display.DataGrid.Refresh(_xml.Controller.SortedAnimes);
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
                foreach (var cell in display.DataGrid.SelectedCells)
                    _xml.Controller.Remove(cell.Item as Anime);
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
            if (Grid.Children.OfType<TextBox>().Any(t => t.Name.Equals("FindBox")))
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

                _xml.Controller.Add(new Anime {
                    Name = display.NameTextbox.Text,
                    Episode = $"{int.Parse(display.EpisodeTextbox.Text):D2}",
                    Status = display.StatusCombobox.Text,
                    Resolution = display.ResolutionCombobox.Text,
                    Airing = display.AiringCheckbox.IsChecked ?? false,
                    NameStrict = display.NameStrictCheckbox.IsChecked ?? false,
                    PreferredSubgroup = subgroup
                });

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

            // Press mouse ButtonSubmit back to go back
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
            display.TorrentTextbox.Text = Path.Combine(display.BaseTextbox.Text, "Torrents");
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

                if (!await Nyaa.IsOnline()) {
                    textBox.Text = ">> Nyaa is currently offline. Try checking later.";
                    this.ToggleButtons();
                }

                else {
                    textBox.Text = ">> Searching for currently airing anime episodes ...\n";
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
            var display = ChangeDisplay<Misc>();

            display.GetAll<RadioButton>().ForEach(r => r.KeyDown += (o, args) => {
                if (args.Key == Key.Enter)
                    display.ButtonSubmit.Press();
            });

            display.ButtonSubmit.Click += async delegate {

                this.ToggleButtons();

                if (display.RadioDownload.IsChecked ?? false) {
                    var count = await _filehandler.DownloadMissing();
                    MessageBox.Show($"Downloaded {count} episodes.");
                }

                else if (display.RadioCatchUp.IsChecked ?? false) {
                    var response =
                        MessageBox.Show(
                            "Please don't do this often, it expends a lot of requests. Are you sure you want to?",
                            "Confirmation", MessageBoxButton.YesNo);

                    if (response == MessageBoxResult.Yes) {
                        var downloadDisplay = ChangeDisplay<Download>();
                        var textBox = downloadDisplay.TextBox;
                        int result;
                        var total = 0;

                        textBox.Text = ">> Attempting to catch up on airing anime episodes ...\n";

                        do {
                            result = await _downloader.DownloadAnime(_xml.Controller.AiringAnimes, textBox, _logger);
                            total += result;
                        } while (result != 0);

                        textBox.AppendText(total > 0
                            ? $">> Found {total} anime downloads."
                            : ">> No new anime found.");
                        textBox.ScrollDown();
                    }

                }

                else if (display.RadioDuplicates.IsChecked ?? false) {
                    var count = await _filehandler.MoveDuplicates();
                    MessageBox.Show($"Moved {count} files to duplicate folder.");
                }

                else if (display.RadioIndexLastWatched.IsChecked ?? false) {
                    _filehandler.IndexAnimesToWatched(_allAnime.Where(a => a.Status.Equals("Watching")));
                    MessageBox.Show("Reset episode order to last known in Watched folder.");
                }

                else if (display.RadioIndexLastUnwatched.IsChecked ?? false) {
                    _filehandler.IndexAnimesToUnwatched(_allAnime.Where(a => a.Status.Equals("Watching")));
                    MessageBox.Show("Reset episode order to last known in any folder.");
                }

                else if (display.RadioIndexFirstWatched.IsChecked ?? false) {
                    _filehandler.ResetKnown(_allAnime.Where(a => a.Status.Equals("Watching")));
                    MessageBox.Show("Reset episode count to first known episode.");
                }

                else if (display.RadioIndexZero.IsChecked ?? false) {
                    foreach (var anime in _allAnime.Where(a => a.Status.Equals("Watching")))
                        anime.Episode = "00";
                    MessageBox.Show("Reset episode count to zero.");
                }

                this.ToggleButtons();
            };
        }
        
    }
}