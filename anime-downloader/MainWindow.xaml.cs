#define WINDOWS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Classes;
using anime_downloader.Classes.File;
using anime_downloader.Classes.Web;
using anime_downloader.Classes.Xml;
using anime_downloader.Views;
using static anime_downloader.Classes.OperatingSystemApi;
using Settings = anime_downloader.Classes.Settings;
using UserControl = System.Windows.Controls.UserControl;

namespace anime_downloader
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     A collection of all the anime.
        /// </summary>
        private List<Anime> _allAnime;

        /// <summary>
        ///     The current display on the right window pane.
        /// </summary>
        private UserControl _currentDisplay;

        /// <summary>
        ///     A helper for modifying anime.
        /// </summary>
        private Anime _currentlyEditedAnime;

        /// <summary>
        ///     Handles downloading operations.
        /// </summary>
        private Downloader _downloader;

        /// <summary>
        ///     Handles tracking/managing files.
        /// </summary>
        private FileHandler _filehandler;

        /// <summary>
        ///     The system tray icon.
        /// </summary>
        private System.Windows.Forms.NotifyIcon _notifyIcon;

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

        public MainWindow()
        {
            CheckIfAlreadyOpen();
            InitializeComponent();
            InitializeSettings();
            InitializeTray();
        }

        // Helper functions

        private static void CheckIfAlreadyOpen()
        {
            const int swRestore = 9;

            // All same exact processes
            var processes = Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(Assembly
                    .GetEntryAssembly()
                    .Location));

            // If more than one window open
            if (processes.Length > 1)
            {
                var hwnd = FindWindow(null, "Anime Downloader");
                ShowWindow(hwnd, swRestore);
                SetForegroundWindow(hwnd);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        ///     Initialize and set the settings object.
        /// </summary>
        private void InitializeSettings()
        {
            _settings = new Settings();
            _playlist = new Playlist(_settings);
            _xml = new Xml(_settings);
            _downloader = new Downloader(_settings);
            _filehandler = new FileHandler(_settings);
            
            if (!Directory.Exists(_settings.ApplicationPath))
                Directory.CreateDirectory(_settings.ApplicationPath);

            // Create new anime xml
            if (!File.Exists(_settings.AnimeXmlPath))
                _xml.Create.AnimeXmlAndSave();

            // Create new settings xml or edit the schema and load anime
            if (!File.Exists(_settings.SettingsXmlPath))
                CreateNewSettings();

            else
            {
                _xml.Verify.SettingsSchema();
                _xml.Verify.AnimeSchema();
                _allAnime = _xml.Controller.FilteredSortedAnimes().ToList();
                ChangeDisplay<Home>();
            }

            
        }

        /// <summary>
        ///     Create the tray.
        /// </summary>
        private void InitializeTray()
        {
            // get the image from the program
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("anime_downloader.ad3.ico");
            Debug.Assert(stream != null, "stream != null");
            var icon = new Icon(stream);

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = icon
            };

            _notifyIcon.Click += delegate
            {
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
        /// <typeparam name="TView">
        ///     A name of a class in the Views folders
        /// </typeparam>
        /// <returns>
        ///     A an instantiated view of type TView
        /// </returns>
        private TView ChangeDisplay<TView>() where TView : UserControl, new()
        {
            // Don't reload the same view
            if (_currentDisplay != null && _currentDisplay.GetType() == typeof (TView))
                 return (TView) _currentDisplay;
            _currentDisplay = new TView();
            Display.Children.Clear();
            Display.Children.Add(_currentDisplay);
            return (TView) _currentDisplay;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                _notifyIcon.Visible = true;
            }
            else if (WindowState == WindowState.Normal)
            {
                Show();
                _notifyIcon.Visible = false;
            }
        }

        private bool CrucialDirectoriesExist()
        {
            var error = string.Empty;

            if (!Directory.Exists(_settings.BaseFolderPath))
                error += "Your base folder doesn't seem to exist.\n";

            if (!File.Exists(_settings.UtorrentPath) || !_settings.UtorrentPath.ToLower().EndsWith(".exe"))
                error += "Your uTorrent.exe path seems to be wrong.";

            if (error.Length > 0)
                Alert(error);

            return error.Length == 0;
        }

        private static void Alert(string msg) => MessageBox.Show(msg);

        // Event Handling

        // Home

        /// <summary>
        ///     View: Home.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonHome_Click(object sender, RoutedEventArgs e)
        {
            ChangeDisplay<Home>();
        }

        /// <summary>
        ///     Event: Open base folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_settings.BaseFolderPath))
                Process.Start(_settings.BaseFolderPath);
            else
                Alert("Your base folder doesn't seem to exist.");
        }

        /// <summary>
        ///     Event: Open settings folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOpenExecuting_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_settings.ApplicationPath);
        }

        // Playlist Creator

        /// <summary>
        ///     View: Playlist Creator.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var playlistDisplay = ChangeDisplay<PlaylistCreator>();
            if (playlistDisplay == null)
                return;

            var path = Path.Combine(_settings.BaseFolderPath, "playlist.m3u");

            playlistDisplay.OpenButton.Click += delegate
            {
                Process.Start(path);
            };

            if (!File.Exists(path))
            {
                playlistDisplay.OpenButton.Toggle();
            }

            playlistDisplay.CreateButton.Click += PlaylistCreateButton_Click;
        }

        /// <summary>
        ///     Event: Submit -> Playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistCreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(_settings.BaseFolderPath))
                Alert("Your base folder doesn't seem to exist.");

            else
            {
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

                Alert("Playlist created.");

                ButtonHome.Press();
                ButtonPlaylist.Press();
            }
        }

        // Anime List & Anime Details

        /// <summary>
        ///     View: Anime list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonList_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<AnimeList>();

            display.FilterComboBox.Text = _settings.FilterBy;
            display.Refresh(_xml.Controller);

            display.FilterComboBox.DropDownClosed += delegate
            {
                _settings.FilterBy = display.FilterComboBox.Text;
                display.Refresh(_xml.Controller);
            };

            display.Add.Click += ButtonAddNew_Click;
            display.Edit.Click += AnimeListEdit;
            display.Delete.Click += AnimeListDelete_Click;
            display.AddMultiple.Click += AnimeListAddMultiple_Click;
            display.DataGrid.PreviewKeyDown += AnimeListDelete_KeyDown;
            display.DataGrid.MouseDoubleClick += AnimeList_MouseDoubleClick;
            display.DataGrid.Sorting += (o, args) =>
            {
                // there's some problem with sorting the rating, this fixes it
                var col = args.Column;
                if (col.Header.Equals("Rating"))
                {
                    if (col.SortDirection == null)
                        Anime.SortedRateFlag = 1;
                    else
                        Anime.SortedRateFlag ^= 1;
                }
            };

            Grid.KeyDown += (o, keyEventArgs) =>
            {
                if (!(_currentDisplay is AnimeList))
                    return;

                if (keyEventArgs.Key == Key.F &&
                    (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    CreateAnimeFindPopup();
                }
            };
        }
        
        /// <summary>
        ///     View: Submit -> Anime list (add)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAddNew_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<AnimeDetails>();
            display.AddButton.Click += ButtonAdd_Click;

            // Enter will create the anime
            KeyEventHandler enterToAdd = (obj, k) =>
            {
                if (k.Key != Key.Enter)
                    return;

                display.AddButton.Focus();
                display.AddButton.Press();
            };

            // Focus the name textbox on load
            display.NameTextbox.Loaded += delegate { display.NameTextbox.Focus(); };

            display.NameTextbox.KeyUp += enterToAdd;
            display.EpisodeTextbox.KeyUp += enterToAdd;
            _settings.Subgroups.ToList().ForEach(s => display.SubgroupComboBox.Items.Add(s));
        }

        /// <summary>
        ///     View: Submit -> Anime list (add multiple)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListAddMultiple_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<AnimeDetailsMultiple>();

            display.InputTextBox.Loaded += delegate { display.InputTextBox.Focus(); };

            display.SubmitButton.Click += delegate
            {
                var names = display.InputTextBox.Text.Split(Environment.NewLine.ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries).Select(n => n.ToLower()).ToList();
                if (names.Distinct().Count() != names.Count)
                    Alert("Names have to be unique.");
                else if (_allAnime.Select(a => a.Name.ToLower()).Intersect(names).Any())
                    Alert("A title entered already exists in the anime list.");
                else
                {
                    foreach (var name in names)
                    {
                        _xml.Controller.Add(new Anime
                        {
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

        /// <summary>
        ///     Event: Chooses view for Edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListEdit(object sender, RoutedEventArgs e)
        {
            var tableDisplay = (AnimeList) _currentDisplay;
            if (tableDisplay.DataGrid.SelectedCells.Count > 1)
                AnimeListEdit_Multiple();
            else if (tableDisplay.DataGrid.SelectedCells.Count == 1)
                AnimeListEdit_Single();
        }

        /// <summary>
        ///     View: Submit -> Anime list (edit)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListEdit_Single()
        {
            var tableDisplay = (AnimeList) _currentDisplay;
            var anime = tableDisplay.DataGrid.SelectedCells.FirstOrDefault().Item as Anime;
            if (anime == null)
                return;

            var display = ChangeDisplay<AnimeDetails>();
            display.AddButton.Content = "Edit";
            display.AddButton.Click += ButtonAnimeEdit_Click;

            // Press enter to add the anime
            KeyEventHandler enterApply = (obj, k) =>
            {
                if (k.Key != Key.Enter)
                    return;
                display.AddButton.Focus();
                display.AddButton.Press();
            };

            // Press Escape to go back
            KeyDown += (o, keyEventArgs) =>
            {
                var key = keyEventArgs.Key;
                if (key == Key.Escape || key == Key.BrowserBack)
                {
                    ButtonHome.Press();
                    ButtonList.Press();
                }
            };

            // Press mouse ButtonSubmit back to go back
            MouseDown += (o, buttonEventArgs) =>
            {
                if (buttonEventArgs.ChangedButton.Equals(MouseButton.XButton1))
                {
                    ButtonHome.Press();
                    ButtonList.Press();
                }
            };

            display.GetAll<TextBox>().ForEach(tb => tb.KeyDown += enterApply);

            display.NameTextbox.Text = anime.Name;
            display.EpisodeTextbox.Text = anime.Episode;

            display.ResolutionContainerGrid.GetAll<RadioButton>()
                .First(radio => radio.Content.Equals(anime.Resolution))
                .IsChecked = true;

            display
                .StatusContainerGrid.GetAll<RadioButton>()
                .First(radio => radio.Content.Equals(anime.Status))
                .IsChecked = true;

            display.AiringCheckbox.IsChecked = anime.Airing;
            display.NameStrictCheckbox.IsChecked = anime.NameStrict;
            display.RatingTextbox.Text = anime.Rating;

            _settings.Subgroups.ToList().ForEach(s => display.SubgroupComboBox.Items.Add(s));
            var subgroup = anime.PreferredSubgroup;
            display.SubgroupComboBox.Text = subgroup != null && subgroup.Equals("") ? "(None)" : subgroup;

            _currentlyEditedAnime = anime;
        }

        /// <summary>
        ///     View: Submit -> Anime list (edit multiple)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListEdit_Multiple()
        {
            var tableDisplay = (AnimeList) _currentDisplay;
            var animes = tableDisplay.DataGrid.SelectedCells.Select(a => a.Item as Anime).Distinct().ToList();
            var display = ChangeDisplay<AnimeDetailsMultiple>();

            // get the most used resolution, status, and airing from the selection,
            // then make them the value in the boxes
            var resolution = animes.GroupBy(a => a.Resolution).OrderByDescending(c => c.Count()).First().Key;
            var status = animes.GroupBy(a => a.Status).OrderByDescending(c => c.Count()).First().Key;
            var airing = animes.GroupBy(a => a.Airing).OrderByDescending(c => c.Count()).First().Key;
            display.StatusComboBox.Text = status;
            display.AiringCheckBox.IsChecked = airing;
            display.ResolutionComboBox.Text = resolution;

            // Change the header and content
            display.InfoTextBlock.Text = "Make the same change to the following list of anime: ";
            display.InputTextBox.Text = string.Join("\n", animes.Select(a => a.Name));
            display.InputTextBox.IsReadOnly = true;

            // Turn the episode box into a rating box
            display.EpisodeLabel.Content = "Rating";
            display.EpisodeTextBox.Text = "";
            display.EpisodeTextBox.PreviewTextInput += (sender, e) =>
            {
                int total;
                int toAdd;

                // Only numbers allowed
                if (display.EpisodeTextBox.Text.Any(c => !char.IsDigit(c)) || e.Text.Any(c => !char.IsDigit(c)))
                {
                    e.Handled = true;
                }

                if (!display.EpisodeTextBox.SelectionLength.Equals(2) &&
                    int.TryParse(display.EpisodeTextBox.Text, out total) && int.TryParse(e.Text, out toAdd))
                {
                    toAdd *= (int) Math.Pow(10, display.EpisodeTextBox.Text.Length + 1);
                    if (total + toAdd > 10 || toAdd == 0)
                    {
                        display.EpisodeTextBox.Text = "10";
                        e.Handled = true;
                        display.EpisodeTextBox.Select(0, 2);
                    }
                }
            };

            display.SubmitButton.Click += delegate
            {
                foreach (var anime in animes)
                {
                    if (!display.EpisodeTextBox.Text.Equals(""))
                        anime.Rating = display.EpisodeTextBox.Text;
                    anime.Status = display.StatusComboBox.Text;
                    anime.Airing = display.AiringCheckBox.IsChecked == true;
                    anime.Resolution = display.ResolutionComboBox.Text;
                }
                ButtonList.Press();
            };
        }

        /// <summary>
        ///     Event: Submit -> Anime list (delete)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListDelete_Click(object sender, RoutedEventArgs e)
        {
            var display = (AnimeList) _currentDisplay;
            foreach (var cell in display.DataGrid.SelectedCells)
                _xml.Controller.Remove(cell.Item as Anime);
            display.Refresh(_xml.Controller);
        }

        /// <summary>
        ///     Event: Keydown -> Anime list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimeListDelete_KeyDown(object sender, KeyEventArgs e)
        {
            var display = (AnimeList) _currentDisplay;

            // Delete
            if (e.Key == Key.Delete)
            {
                foreach (var cell in display.DataGrid.SelectedCells)
                    _xml.Controller.Remove(cell.Item as Anime);
                display.Refresh(_xml.Controller);
            }

            // Edit
            else if (e.Key == Key.Enter)
            {
                if (display.DataGrid.SelectedCells.FirstOrDefault().IsValid)
                {
                    AnimeListEdit(sender, e);
                }
            }
        }

        /// <summary>
        ///     Secondary View: Find anime box
        /// </summary>
        private void CreateAnimeFindPopup()
        {
            var display = (AnimeList) _currentDisplay;

            // Don't recreate it again
            if (Grid.Children.OfType<TextBox>().Any(t => t.Name.Equals("FindBox")))
                return;

            var findWindow = new TextBox
            {
                Name = "FindBox",
                Width = 400,
                Height = 30,
                Margin = new Thickness(450, 250, 0, 0),
                FontSize = 18
            };

            // Reset values and remove the find
            RoutedEventHandler closeFindWindow = delegate
            {
                Grid.Children.Remove(findWindow);
                display.DataGrid.ItemsSource = _xml.Controller.FilteredSortedAnimes();
                display.DataGrid.Focus();
            };

            MouseButtonEventHandler closeFindWindowMouse = delegate
            {
                Grid.Children.Remove(findWindow);
                display.DataGrid.ItemsSource = _xml.Controller.FilteredSortedAnimes();
                display.DataGrid.Focus();
            };

            // --> Closing the find
            // Make any button press close the find window, and going into anime details too
            this.GetAll<Button>().ForEach(b => b.Click += closeFindWindow);
            display.DataGrid.MouseDoubleClick += closeFindWindowMouse;

            // CTRL-F again or Escape also close find
            Grid.KeyDown += (sender, keyEventArgs) =>
            {
                if (!(_currentDisplay is AnimeList))
                    return;

                if (keyEventArgs.Key == Key.Escape)
                    closeFindWindow(sender, keyEventArgs);
                else if (keyEventArgs.Key == Key.F && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    if (findWindow.IsSelectionActive)
                        closeFindWindow(sender, keyEventArgs);
                    else
                        findWindow.Focus();
                }
            };

            // --> The actual functionality
            findWindow.KeyUp += delegate
            {
                var text = findWindow.Text.ToLower().Trim();
                var copy = _xml.Controller.FilteredSortedAnimes().Where(a => a.Name.ToLower().Contains(text));
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
        private void AnimeList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var display = (AnimeList) _currentDisplay;
            var selected = display.DataGrid.SelectedCells.FirstOrDefault();
            if (selected.IsValid)
            {
                AnimeListEdit(sender, e);
            }
        }

        // AnimeDetails

        /// <summary>
        ///     Event: Submit -> AnimeDetails (add)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            var display = (AnimeDetails) _currentDisplay;

            if (display.NameTextbox.Empty())
                Alert("There needs to be a name.");

            else
            {
                var subgroup = display.SubgroupComboBox.Text;
                if (subgroup.Equals("(None)"))
                    subgroup = "";

                var episode = display.EpisodeTextbox.Text.Length > 0
                    ? $"{int.Parse(display.EpisodeTextbox.Text):D2}"
                    : "00";

                var status = display
                    .StatusContainerGrid.GetAll<RadioButton>()
                    .First(radio => radio.IsChecked != null && radio.IsChecked.Value)
                    .Content.ToString();

                var resolution = display
                    .ResolutionContainerGrid.GetAll<RadioButton>()
                    .First(radio => radio.IsChecked != null && radio.IsChecked.Value)
                    .Content.ToString();

                _xml.Controller.Add(new Anime
                {
                    Name = display.NameTextbox.Text,
                    Episode = episode,
                    Status = status,
                    Resolution = resolution,
                    Airing = display.AiringCheckbox.IsChecked ?? false,
                    NameStrict = display.NameStrictCheckbox.IsChecked ?? false,
                    PreferredSubgroup = subgroup,
                    Rating = display.RatingTextbox.Text
                });

                ButtonList.Press();
            }
        }

        /// <summary>
        ///     Event: Submit -> AnimeDetails (edit)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAnimeEdit_Click(object sender, RoutedEventArgs e)
        {
            var display = (AnimeDetails) _currentDisplay;

            if (display.NameTextbox.Empty())
                Alert("There needs to be a name.");

            else
            {
                var subgroup = display.SubgroupComboBox.Text;

                var episode = display.EpisodeTextbox.Text.Length > 0
                    ? $"{int.Parse(display.EpisodeTextbox.Text):D2}"
                    : "00";

                var status = display
                    .StatusContainerGrid.GetAll<RadioButton>()
                    .First(radio => radio.IsChecked != null && radio.IsChecked.Value)
                    .Content.ToString();

                var resolution = display
                    .ResolutionContainerGrid.GetAll<RadioButton>()
                    .First(radio => radio.IsChecked != null && radio.IsChecked.Value)
                    .Content.ToString();

                _currentlyEditedAnime.Name = display.NameTextbox.Text;
                _currentlyEditedAnime.Episode = episode;
                _currentlyEditedAnime.Status = status;
                _currentlyEditedAnime.Resolution = resolution;
                _currentlyEditedAnime.Airing = display.AiringCheckbox.IsChecked ?? false;
                _currentlyEditedAnime.NameStrict = display.NameStrictCheckbox.IsChecked ?? false;
                _currentlyEditedAnime.PreferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup;
                _currentlyEditedAnime.Rating = display.RatingTextbox.Text;
                ButtonList.Press();
            }
        }

        // Settings

        /// <summary>
        ///     View: Settings (edit)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Views.Settings>();

            display.GetAll<TextBox>().ForEach(t => t.KeyUp += (o, k) =>
            {
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
        private void ButtonApplySettings_Click(object sender, RoutedEventArgs e)
        {
            var display = (Views.Settings) _currentDisplay;

            if (display.BaseTextbox.Empty() || display.TorrentTextbox.Empty() || display.DownloadTextbox.Empty())
                Alert("You must enter in Base, Torrent or Utorrent Path Boxes.");

            else
            {
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
        private void CreateNewSettings()
        {
            this.ToggleButtons();
            var display = ChangeDisplay<Views.Settings>();
            display.ApplyChangesButton.Toggle();

            // Default guessed values
            display.BaseTextbox.Text = Directory.GetCurrentDirectory();
            display.TorrentTextbox.Text = Path.Combine(display.BaseTextbox.Text, "Torrents");
            display.DownloadTextbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
            display.ApplyChangesButton.Content = "Create Profile";

            display.ApplyChangesButton.Click += (obj, ev) =>
            {
                if (display.BaseTextbox.Empty() || display.TorrentTextbox.Empty() || display.DownloadTextbox.Empty())
                    Alert("You must enter in Base, Torrent or Utorrent Path Boxes.");

                else
                {
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

                    _allAnime = _xml.Controller.FilteredSortedAnimes().ToList();
                    this.ToggleButtons();
                    ChangeDisplay<Home>();
                }
            };
        }

        // DownloadOptions & DownloadOutput

        /// <summary>
        ///     View: DownloadOptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<DownloadOptions>();

            display.SearchButton.Click += async delegate
            {
                this.ToggleButtons();

                if (display.CheckForLatestRadio.IsChecked == true)
                    await CheckForLatest();

                else if(display.GetUpToDateRadio.IsChecked == true)
                    await GetUpToDate();

                else if (display.GetMissingRadio.IsChecked == true)
                    await GetMissingEpisodes();

                this.ToggleButtons();
            };

        }
        
        /// <summary>
        ///     View: DownloadOutput (Check for latest anime)
        /// </summary>
        private async Task CheckForLatest()
        {
            if (CrucialDirectoriesExist())
            {
                if (!Directory.Exists(_settings.TorrentFilesPath))
                    Directory.CreateDirectory(_settings.TorrentFilesPath);

                var downloadDisplay = ChangeDisplay<DownloadOutput>();
                var textbox = downloadDisplay.TextBox;

                if (!await Nyaa.IsOnline())
                    textbox.WriteLine(">> Nyaa is currently offline. Try checking later.");

                else
                {
                    textbox.WriteLine(">> Searching for currently airing anime episodes ...");
                    var downloaded = await _downloader.DownloadAsync(_xml.Controller.AiringAnimes.ToList(), textbox);
                    textbox.WriteLine(downloaded > 0
                        ? $">> Found {downloaded} anime downloads."
                        : ">> No new anime found.");
                }
            }
        }

        /// <summary>
        ///     View: DownloadOutput (Get up to date)
        /// </summary>
        /// <returns></returns>
        private async Task GetUpToDate()
        {
            var response = MessageBox.Show(
                "Please don't do this often, it expends a lot of requests. Are you sure you want to?",
                "Confirmation",
                MessageBoxButton.YesNo);

            if (response == MessageBoxResult.Yes)
            {
                var display = ChangeDisplay<DownloadOutput>();
                var textBox = display.TextBox;
                int result;
                var total = 0;

                textBox.Text = ">> Attempting to catch up on airing anime episodes ...\n";

                do
                {
                    result = await _downloader.DownloadAsync(_xml.Controller.AiringAnimes.ToList(), textBox);
                    total += result;
                } while (result != 0);

                textBox.AppendText(total > 0
                    ? $">> Found {total} anime downloads."
                    : ">> No new anime found.");
                textBox.ScrollDown();
            }
        }

        /// <summary>
        ///     View: DownloadOutput (Download missing episodes)
        /// </summary>
        /// <remarks>
        ///     Find and download any episodes in collection anime that are between the range 
        ///     start.episode and last.episode
        /// </remarks>
        /// <returns></returns>
        private async Task GetMissingEpisodes()
        {
            var display = ChangeDisplay<DownloadOutput>();
            var textBox = display.TextBox;
            textBox.Text = ">> Finding all missing episodes ...\n";
            var allEpisodeFiles = await Task.Run(() => _filehandler.AllAnime().ToList());
            var animeFileDeltas = await Task.Run(() =>
                _filehandler.FirstEpisodes(allEpisodeFiles)
                    .OrderBy(a => a.Name)
                    .Zip(_filehandler.LastEpisodes(allEpisodeFiles).OrderBy(a => a.Name),
                        (a, b) => new AnimeEpisodeDelta(a, b)));
            var total = await _downloader.DownloadAsync(_allAnime.Watching(), animeFileDeltas, allEpisodeFiles, textBox);

            textBox.AppendText(total > 0
                ? $">> Found {total} anime downloads."
                : ">> No new anime found.");
            textBox.ScrollDown();
        }

        // Misc

        /// <summary>
        ///     View: Misc
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonMisc_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Misc>();
            display.GetAll<RadioButton>().ForEach(r => r.KeyDown += (o, args) =>
            {
                if (args.Key == Key.Enter)
                    display.ButtonSubmit.Press();
            });

            display.ButtonSubmit.Click += ButtonMisc_Submit;

            display.AnidbButton.Click += delegate
            {
                Process.Start("https://anidb.net/");
            };

            display.AnichartButton.Click += delegate
            {
                Process.Start("http://anichart.net/");
            };

            display.MyanimelistButton.Click += delegate
            {
                Process.Start("http://myanimelist.net/");
            };

            display.NyaaButton.Click += delegate
            {
                Process.Start("https://www.nyaa.se/");
            };

        }

        private async void ButtonMisc_Submit(object sender, RoutedEventArgs e)
        {
            this.ToggleButtons();

            var display = (Misc) _currentDisplay;

            if (display.RadioDuplicates.IsChecked == true)
            {
                var count = await _filehandler.MoveDuplicates();
                Alert($"Moved {count} files to duplicate folder.");
            }

            else if (display.RadioIndexLastWatched.IsChecked == true)
            {
                _filehandler.AnimesToLastEpisode_Watched(_allAnime.Watching());
                Alert("Reset episode order to last known in Watched folder.");
            }

            else if (display.RadioIndexLastUnwatched.IsChecked == true)
            {
                _filehandler.AnimesToLastEpisode_Unwatched(_allAnime.Watching());
                Alert("Reset episode order to last known in any folder.");
            }

            else if (display.RadioIndexFirstWatched.IsChecked == true)
            {
                _filehandler.AnimesToBeginningEpisode_All(_allAnime.Watching());
                Alert("Reset episode count to first known episode.");
            }

            else if (display.RadioIndexZero.IsChecked == true)
            {
                foreach (var anime in _allAnime.Watching())
                    anime.Episode = "00";
                Alert("Reset episode count to zero.");
            }

            this.ToggleButtons();
        }
    }
}