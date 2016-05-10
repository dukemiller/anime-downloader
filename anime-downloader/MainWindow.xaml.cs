using anime_downloader.Classes;
using anime_downloader.Classes.File;
using anime_downloader.Classes.Web;
using anime_downloader.Classes.Xml;
using anime_downloader.Views;
using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static anime_downloader.Classes.OperatingSystemApi;
using Settings = anime_downloader.Classes.Settings;

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
        ///     Handle playlist creation with some customization.
        /// </summary>
        private Playlist _playlist;

        /// <summary>
        ///     Handles paths and user settings.
        /// </summary>
        private Settings _settings;

        /// <summary>
        ///     Handles logic related to creating and features of the system tray.
        /// </summary>
        private Tray _tray;

        /// <summary>
        ///     Handles objects for modifying and creating the xml files
        /// </summary>
        private Xml _xml;

        public MainWindow()
        {
            HandleIfAlreadyOpened();
            InitializeComponent();
            InitializeSettings();
        }

        /// <summary>
        ///     The current display on the right window pane.
        /// </summary>
        public UserControl CurrentDisplay { get; private set; }

        /* Initializations  */

        private static void HandleIfAlreadyOpened()
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
                Application.Current.Shutdown();
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
            _tray = new Tray(this, _settings);

            if (!Directory.Exists(_settings.ApplicationDirectory))
                Directory.CreateDirectory(_settings.ApplicationDirectory);

            // Create new anime xml
            if (!File.Exists(_settings.AnimeXml))
                _xml.Schema.AnimeXmlAndSave();

            // Create new settings xml or edit the schema and load anime
            if (!File.Exists(_settings.SettingsXml))
                CreateNewSettings();
            else
                InitialState();
        }

        /// <summary>
        ///     To "refresh" views by rapidly cycling from home to button b.
        /// </summary>
        public void HomeCycle(ToggleButton b)
        {
            HomeButton.Press();
            b.Press();
            b.IsChecked = true;
        }

        private void InitialState()
        {
            _xml.Verify.Schema();
            _allAnime = _xml.Controller.FilteredSortedAnimes().ToList();
            _settings.Loaded = true;
            _tray.Initialize();
            ChangeDisplay<Home>();
            
            KeyDown += (o, e) =>
            {
                // So you can type without changing the view
                if (!this.GetAll<TextBox>().Any(t => t.IsFocused))
                {
                    if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                        HomeCycle(HomeButton);
                    else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                        HomeCycle(AnimeListButton);
                    else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                        HomeCycle(SettingsButton);
                    else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
                        HomeCycle(DownloadButton);
                    else if (e.Key == Key.D5 || e.Key == Key.NumPad5)
                        HomeCycle(PlaylistsButton);
                    else if (e.Key == Key.D6 || e.Key == Key.NumPad6)
                        HomeCycle(WebButton);
                    else if (e.Key == Key.D7 || e.Key == Key.NumPad7)
                        HomeCycle(MiscButton);
                }
            };
        }

        /* Helper functions */

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
            if (CurrentDisplay != null && CurrentDisplay.GetType() == typeof(TView))
                return (TView) CurrentDisplay;
            CurrentDisplay = new TView();
            Display.Children.Clear();
            Display.Children.Add(CurrentDisplay);
            return (TView) CurrentDisplay;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (_settings.Loaded)
            {
                // Necessary for bringing focus from another application
                if (WindowState == WindowState.Normal)
                    Show();
                else if (WindowState == WindowState.Minimized) // && (_settings.ToTrayOnMinimize))
                    Hide();

                _tray.CheckVisibility();
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_settings.Loaded)
            {
                if (_settings.ExitOnClose)
                    _tray.Visible = false;
                else
                {
                    WindowState = WindowState.Minimized;
                    e.Cancel = true;
                }
            }
        }

        private bool CrucialDirectoriesExist()
        {
            var error = string.Empty;

            if (!Directory.Exists(_settings.BaseDirectory))
                error += "Your base folder doesn't seem to exist.\n";

            if (!File.Exists(_settings.UtorrentFile) || !_settings.UtorrentFile.ToLower().EndsWith(".exe"))
                error += "Your uTorrent.exe path seems to be wrong.";

            if (error.Length > 0)
                Alert(error);

            return error.Length == 0;
        }

        private static void Alert(string msg = "") => MessageBox.Show(msg);
        
        /* Event Handling */

        /* --Home */

        /// <summary>
        ///     View: Home.
        /// </summary>
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeDisplay<Home>();
        }

        /* --Playlist Creator */

        /// <summary>
        ///     View: Playlist Creator.
        /// </summary>
        private void PlaylistsButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<PlaylistCreator>();

            if (!File.Exists(_settings.PlaylistFile))
            {
                display.OpenButton.Toggle();
            }

            display.KeyDown += (o, args) =>
            {
                if (args.Key == Key.Enter)
                    display.CreateButton.Press();
            };
            display.OpenButton.Click += delegate
            {
                if (File.Exists(_settings.PlaylistFile))
                    Process.Start(_settings.PlaylistFile);
            };
            display.CreateButton.Click += Playlist_PlaylistCreateButton_Click;
            display.Loaded += delegate { display.CreateButton.Focus(); };
        }

        /// <summary>
        ///     Event: Submit -> Playlist
        /// </summary>
        private void Playlist_PlaylistCreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (CrucialDirectoriesExist())
            {
                _playlist.Refresh();

                if (_playlist.Length == 0)
                {
                    Alert("No playlist created (no files were found in the episode folders).");
                }
                else
                {
                    var display = (PlaylistCreator) CurrentDisplay;

                    if (display.EpisodeRadio.IsChecked == true)
                        _playlist.ByEpisodeNumber();
                    else if (display.MomentRadio.IsChecked == true)
                        _playlist.ByDate();

                    // else pass

                    if (display.SeparateCheckBox.IsChecked == true)
                        _playlist.SeparateShowOrder();

                    if (display.ReverseCheckbox.IsChecked == true)
                        _playlist.Reverse();

                    _playlist.Save();

                    Alert("Playlist created.");
                }

                HomeCycle(PlaylistsButton);
            }
        }

        /* --Anime List */

        /// <summary>
        ///     View: AnimeList
        /// </summary>
        private void AnimeListButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDisplay is AnimeList)
                return;

            var display = ChangeDisplay<AnimeList>();

            display.Loaded += delegate
            {
                ((AnimeList) CurrentDisplay).DataGrid.Focus();
            };

            display.Refresh(_xml.Controller);
            display.FilterComboBox.Text = _settings.FilterBy;
            display.FilterComboBox.DropDownClosed += delegate
            {
                _settings.FilterBy = display.FilterComboBox.Text;
                display.Refresh(_xml.Controller);
                CloseAnimeFindPopup();
            };

            display.Add.Click += AnimeList_Context_Add_Click;
            display.Edit.Click += AnimeListEdit;
            display.Delete.Click += AnimeList_Context_Delete_Click;
            display.AddMultiple.Click += AnimeList_Context_AddMultiple_Click;
            display.Search.Click += AnimeList_Context_Search_Click;
            display.DataGrid.PreviewKeyDown += AnimeList_KeyDown;
            display.DataGrid.MouseDoubleClick += AnimeList_MouseDoubleClick;
            display.FindRectangle.MouseDown += delegate
            {
                var findBox = Display.Children.OfType<TextBox>().FirstOrDefault(t => t.Name.Equals("FindBox"));
                if (findBox == null)
                    CreateAnimeFindPopup();
                else
                    CloseAnimeFindPopup();
            };

            MainGrid.KeyDown += (o, keyEventArgs) =>
            {
                if (!(CurrentDisplay is AnimeList))
                    return;

                if (keyEventArgs.Key == Key.F &&
                    (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    CreateAnimeFindPopup();
                }
            };
        }

        public async void AnimeList_Context_Search_Click(object sender, RoutedEventArgs e)
        {
            var display = (AnimeList) CurrentDisplay;
            await SearchOnNyaa(((Anime) display.DataGrid.SelectedCells.First().Item).Name);
        }

        /// <summary>
        ///     View: AnimeList -> (Right-Click) Context -> Add : Click
        /// </summary>
        private void AnimeList_Context_Add_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<AnimeDetails>();
            display.AddAnimeButton.Click += AnimeDetails_AddAnimeButton_Click;

            // Enter will create the anime
            KeyEventHandler enterToAdd = (obj, k) =>
            {
                if (k.Key != Key.Enter)
                    return;

                display.AddAnimeButton.Focus();
                display.AddAnimeButton.Press();
            };

            // Focus the name textBox on load
            display.NameTextbox.Loaded += delegate { display.NameTextbox.Focus(); };

            display.NameTextbox.KeyUp += enterToAdd;
            display.EpisodeTextbox.KeyUp += enterToAdd;
            _settings.Subgroups.ToList().ForEach(s => display.SubgroupComboBox.Items.Add(s));
            display.OpenLastButton.Visibility = Visibility.Hidden;
        }

        /// <summary>
        ///     View: AnimeList -> (Right-Click) Context -> Add Multiple : Click
        /// </summary>
        private void AnimeList_Context_AddMultiple_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<AnimeDetailsMultiple>();
            display.RatingTextBox.IsHitTestVisible = false;
            display.RatingTextBox.Opacity = 0.40;

            display.InputTextBox.Loaded += delegate { ((AnimeDetailsMultiple) CurrentDisplay).InputTextBox.Focus(); };

            display.SubmitButton.Click += delegate
            {
                var names =
                    display.InputTextBox.Text.Split(Environment.NewLine.ToCharArray(),
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
                    AnimeListButton.Press();
                }
            };
        }

        /// <summary>
        ///     Event: Animelist -> (Right-Click) Context -> Delete : Click
        /// </summary>
        private void AnimeList_Context_Delete_Click(object sender, RoutedEventArgs e)
        {
            var display = (AnimeList) CurrentDisplay;
            foreach (var cell in display.DataGrid.SelectedCells)
                _xml.Controller.Remove(cell.Item as Anime);
            display.Refresh(_xml.Controller);
        }

        /// <summary>
        ///     Event: Keydown
        /// </summary>
        private void AnimeList_KeyDown(object sender, KeyEventArgs e)
        {
            var display = (AnimeList) CurrentDisplay;

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
        ///     Event: Double click
        /// </summary>
        private void AnimeList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var display = CurrentDisplay as AnimeList;
            if (display == null)
                return;
            var selected = display.DataGrid.SelectedCells.FirstOrDefault();
            if (selected.IsValid)
            {
                AnimeListEdit(sender, e);
            }
        }

        /// <summary>
        ///     Event: Chooses view for Edit
        /// </summary>
        private void AnimeListEdit(object sender, RoutedEventArgs e)
        {
            var tableDisplay = (AnimeList) CurrentDisplay;
            if (tableDisplay.DataGrid.SelectedCells.Count > 1)
                AnimeDetails_Multiple();
            else if (tableDisplay.DataGrid.SelectedCells.Count == 1)
                AnimeDetails_Single();
        }

        private void CloseAnimeFindPopup()
        {
            var findWindow = Display.Children.OfType<TextBox>().FirstOrDefault(t => t.Name.Equals("FindBox"));

            if (findWindow != null)
                Display.Children.Remove(findWindow);

            var display = CurrentDisplay as AnimeList;

            if (display != null)
            {
                display.DataGrid.ItemsSource = _xml.Controller.FilteredSortedAnimes();
                display.DataGrid.Focus();
            }
        }

        /// <summary>
        ///     Secondary View: Find anime box
        /// </summary>
        private void CreateAnimeFindPopup()
        {
            var display = (AnimeList) CurrentDisplay;

            // Don't recreate it again
            if (Display.Children.OfType<TextBox>().Any(t => t.Name.Equals("FindBox")))
                return;

            var find = new TextBox
            {
                Name = "FindBox",
                Width = 400,
                Height = 30,
                Margin = new Thickness(270, 270, 0, 0),
                FontSize = 18,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            // --> Closing the find
            // Make any button press close the find window, and going into anime details too
            this.GetAll<Button>().ForEach(b => b.Click += (sender, args) => CloseAnimeFindPopup());
            display.DataGrid.MouseDoubleClick += (sender, args) => CloseAnimeFindPopup();

            // CTRL-F again or Escape also close find
            Display.KeyDown += (sender, keyEventArgs) =>
            {
                if (!(CurrentDisplay is AnimeList))
                    return;

                if (keyEventArgs.Key == Key.Escape)
                    CloseAnimeFindPopup();
                else if (keyEventArgs.Key == Key.F && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (find.IsSelectionActive)
                        CloseAnimeFindPopup();
                    else
                        find.Focus();
                }
            };

            // --> The actual functionality
            find.KeyUp += delegate
            {
                var q = find.Text.ToLower().Trim();
                var result = _xml.Controller.FilteredSortedAnimes().Where(a => a.Name.ToLower().Contains(q));
                display.DataGrid.ItemsSource = result;
            };

            Display.Children.Add(find);
            find.Focus();
        }

        /* --AnimeDetails & AnimeDetailsMultiple */

        /// <summary>
        ///     View: AnimeList -> (Right-Click) Context -> Edit
        /// </summary>
        private void AnimeDetails_Single()
        {
            var tableDisplay = (AnimeList) CurrentDisplay;
            var anime = tableDisplay.DataGrid.SelectedCells.FirstOrDefault().Item as Anime;
            if (anime == null)
                return;

            var display = ChangeDisplay<AnimeDetails>();
            display.AddAnimeButton.Content = "Edit";
            display.AddAnimeButton.Click += AnimeDetails_EditAnimeButton_Click;

            // Press enter to add the anime
            KeyEventHandler enterApply = (obj, k) =>
            {
                if (k.Key != Key.Enter)
                    return;
                display.AddAnimeButton.Focus();
                display.AddAnimeButton.Press();
            };

            // Press Escape to go back
            KeyDown += (o, keyEventArgs) =>
            {
                var key = keyEventArgs.Key;
                if (key == Key.Escape || key == Key.BrowserBack)
                {
                    HomeCycle(AnimeListButton);
                }
            };

            // Press mouse ButtonSubmit back to go back
            MouseDown += (o, buttonEventArgs) =>
            {
                if (buttonEventArgs.ChangedButton.Equals(MouseButton.XButton1))
                {
                    HomeCycle(AnimeListButton);
                }
            };

            display.GetAll<TextBox>().ForEach(tb => tb.KeyDown += enterApply);

            display.NameTextbox.Text = anime.Name;
            display.EpisodeTextbox.Text = anime.Episode;

            display.ResolutionContainerGrid.GetAll<RadioButton>()
                .First(radio => radio.Content.Equals(anime.Resolution))
                .IsChecked = true;

            display.StatusContainerGrid.GetAll<RadioButton>()
                .First(radio => radio.Content.Equals(anime.Status))
                .IsChecked = true;

            display.AiringCheckbox.IsChecked = anime.Airing;
            display.NameStrictCheckbox.IsChecked = anime.NameStrict;
            display.RatingTextbox.Text = anime.Rating;

            _settings.Subgroups.ToList().ForEach(s => display.SubgroupComboBox.Items.Add(s));
            var subgroup = anime.PreferredSubgroup;
            display.SubgroupComboBox.Text = subgroup != null && subgroup.Equals("") ? "(None)" : subgroup;

            display.OpenLastButton.Click += delegate
            {
                var episode = _filehandler.LastEpisodeOf(anime);
                if (episode == null)
                    Alert($"Episode {anime.Episode} for '{anime.Name}' not found in any directory.");
                else
                    Process.Start($"{episode.FilePath}");
            };

            _currentlyEditedAnime = anime;
        }

        /// <summary>
        ///     View: Submit -> Anime list (edit multiple)
        /// </summary>
        private void AnimeDetails_Multiple()
        {
            var tableDisplay = (AnimeList) CurrentDisplay;
            var animes = tableDisplay.DataGrid.SelectedCells.Select(a => a.Item as Anime).Distinct().ToList();
            var display = ChangeDisplay<AnimeDetailsMultiple>();
            display.EpisodeTextBox.Text = "";

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
            
            display.SubmitButton.Click += delegate
            {
                foreach (var anime in animes)
                {
                    if (!display.RatingTextBox.Text.Equals(""))
                        anime.Rating = display.RatingTextBox.Text;
                    if (!display.EpisodeTextBox.Text.Equals(""))
                        anime.Episode = display.EpisodeTextBox.Text;
                    anime.Status = display.StatusComboBox.Text;
                    anime.Airing = display.AiringCheckBox.IsChecked == true;
                    anime.Resolution = display.ResolutionComboBox.Text;
                }
                AnimeListButton.Press();
            };
        }

        /// <summary>
        ///     Event: AnimeDetails -> Add : Click
        /// </summary>
        private void AnimeDetails_AddAnimeButton_Click(object sender, RoutedEventArgs e)
        {
            var display = (AnimeDetails) CurrentDisplay;

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

                var status =
                    display.StatusContainerGrid.GetAll<RadioButton>()
                        .First(radio => radio.IsChecked != null && radio.IsChecked.Value)
                        .Content.ToString();

                var resolution =
                    display.ResolutionContainerGrid.GetAll<RadioButton>()
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

                AnimeListButton.Press();
            }
        }

        /// <summary>
        ///     Event: AnimeDetails -> Edit : Click
        /// </summary>
        private void AnimeDetails_EditAnimeButton_Click(object sender, RoutedEventArgs e)
        {
            var display = (AnimeDetails) CurrentDisplay;

            if (display.NameTextbox.Empty())
                Alert("There needs to be a name.");
            else
            {
                var subgroup = display.SubgroupComboBox.Text;

                var episode = display.EpisodeTextbox.Text.Length > 0
                    ? $"{int.Parse(display.EpisodeTextbox.Text):D2}"
                    : "00";

                var status = display.StatusContainerGrid.GetAll<RadioButton>()
                    .First(radio => radio.IsChecked != null && radio.IsChecked.Value)
                    .Content.ToString();

                var resolution = display.ResolutionContainerGrid.GetAll<RadioButton>()
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
                AnimeListButton.Press();
            }
        }

        /* --Settings */

        /// <summary>
        ///     View: Settings (edit)
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Views.Settings>();

            display.GetAll<TextBox>().ForEach(t => t.KeyUp += (o, k) =>
            {
                if (k.Key == Key.Enter)
                    display.ApplyChangesButton.Press();
            });
            
            display.BaseTextbox.Text = _settings.BaseDirectory;
            display.SubgroupsTextbox.Text = string.Join(", ", _settings.Subgroups);
            display.DownloadTextbox.Text = _settings.UtorrentFile;
            display.TorrentTextbox.Text = _settings.TorrentFilesDirectory;
            display.ApplyChangesButton.Click += Settings_ApplySettingsButton_Click;
            display.OnlyWhitelistedCheckbox.IsChecked = _settings.OnlyWhitelisted;
            display.UseLoggerCheckbox.IsChecked = _settings.UseLogging;
            display.TrayExitCheckbox.IsChecked = !_settings.ExitOnClose;
            display.AlwaysTrayCheckbox.IsChecked = _settings.AlwaysShowTray;
            display.IndividualShowCheckbox.IsChecked = _settings.IndividualShowFolders;

            if (_settings.GroupDownloadBy.Equals("PerWeek"))
                display.PerWeekRadio.IsChecked = true;
            else
                display.SingleFolderRadio.IsChecked = true;
        }

        /// <summary>
        ///     Event: Submit -> Settings (edit)
        /// </summary>
        private void Settings_ApplySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var display = (Views.Settings) CurrentDisplay;

            if (display.BaseTextbox.Empty() || display.TorrentTextbox.Empty() || display.DownloadTextbox.Empty())
                Alert("You must enter in Base, Torrent or Utorrent Path Boxes.");
            else
            {
                _settings.Subgroups = display.SubgroupsTextbox.Text.Split(new[] { ", " },
                    StringSplitOptions.RemoveEmptyEntries);
                _settings.BaseDirectory = display.BaseTextbox.Text;
                _settings.UtorrentFile = display.DownloadTextbox.Text;
                _settings.TorrentFilesDirectory = display.TorrentTextbox.Text;
                _settings.OnlyWhitelisted = display.OnlyWhitelistedCheckbox.IsChecked ?? false;
                _settings.UseLogging = display.UseLoggerCheckbox.IsChecked ?? false;
                _settings.ExitOnClose = !display.TrayExitCheckbox.IsChecked ?? false;
                _settings.AlwaysShowTray = display.AlwaysTrayCheckbox.IsChecked ?? false;

                _tray.CheckVisibility();

                if (display.PerWeekRadio.IsChecked == true)
                    _settings.GroupDownloadBy = "PerWeek";
                else if (display.SingleFolderRadio.IsChecked == true)
                    _settings.GroupDownloadBy = "Single";

                _settings.IndividualShowFolders = display.IndividualShowCheckbox.IsChecked ?? false;
            }
        }

        /// <summary>
        ///     View: Settings (new)
        /// </summary>
        private void CreateNewSettings()
        {
            this.ToggleButtons();
            var display = ChangeDisplay<Views.Settings>();

            // Default guessed values
            display.BaseTextbox.Text = Directory.GetCurrentDirectory();
            display.TorrentTextbox.Text = Path.Combine(display.BaseTextbox.Text, "Torrents");
            display.DownloadTextbox.Text = @"C:\Program Files (x86)\uTorrent\uTorrent.exe";
            display.ApplyChangesButton.Content = "Create";
            display.UseLoggerCheckbox.IsChecked = true;

            display.ApplyChangesButton.Click += (obj, ev) =>
            {
                if (display.BaseTextbox.Empty() || display.TorrentTextbox.Empty() || display.DownloadTextbox.Empty())
                    Alert("You must enter in Base, Torrent or Utorrent Path Boxes.");
                else
                {
                    _xml.Schema.SettingsXmlAndSave();

                    _settings.BaseDirectory = display.BaseTextbox.Text;
                    _settings.TorrentFilesDirectory = display.TorrentTextbox.Text;
                    _settings.UtorrentFile = display.DownloadTextbox.Text;
                    _settings.Subgroups = display.SubgroupsTextbox.Text.Split(new[] { " " },
                        StringSplitOptions.RemoveEmptyEntries);
                    _settings.OnlyWhitelisted = display.OnlyWhitelistedCheckbox.IsChecked ?? false;
                    _settings.UseLogging = display.UseLoggerCheckbox.IsChecked ?? false;
                    _settings.ExitOnClose = !display.TrayExitCheckbox.IsChecked ?? false;
                    _settings.AlwaysShowTray = display.AlwaysTrayCheckbox.IsChecked ?? false;
                    _settings.SortBy = "name"; // TODO

                    this.ToggleButtons();
                    InitialState();
                }
            };
        }

        /* --DownloadOptions & DownloadOutput */

        /// <summary>
        ///     View: DownloadOptions
        /// </summary>
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<DownloadOptions>();

            display.SearchButton.Click += DownloadOutputHandler;
            display.Loaded += (o, args) => ((DownloadOptions) CurrentDisplay).SearchButton.Focus();
            display.KeyDown += (o, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    ((DownloadOptions) CurrentDisplay).SearchButton.Focus();
                    ((DownloadOptions) CurrentDisplay).SearchButton.Press();
                }
            };
        }

        /// <summary>
        ///     View: DownloadOutput
        /// </summary>
        public async void DownloadOutputHandler(object sender, RoutedEventArgs e)
        {
            this.ToggleButtons();

            var display = (DownloadOptions) CurrentDisplay;

            if (display.GetAll<RadioButton>().Any(r => r.IsChecked == true))
            {
                var downloadDisplay = ChangeDisplay<DownloadOutput>();
                var textBox = downloadDisplay.TextBox;

                if (await Nyaa.IsOnlineAsync())
                {
                    try
                    {
                        if (display.CheckForLatestRadio.IsChecked == true)
                            await CheckForLatestAsync(textBox);

                        else if (display.GetUpToDateRadio.IsChecked == true)
                            await GetUpToDateAsync(textBox);

                        else if (display.GetMissingRadio.IsChecked == true)
                            await GetMissingEpisodesAsync(textBox);

                    }

                    catch (Exception)
                    {
                        textBox.WriteLine(">> An error occured while attempting to download, try again.");
                    }
                }

                else
                {
                    textBox.WriteLine(">> Nyaa is currently offline. Try checking later.");
                }
                
            }

            this.ToggleButtons();
        }

        /// <summary>
        ///     DownloadOutput (Check for latest anime)
        /// </summary>
        private async Task CheckForLatestAsync(TextBox textBox)
        {
            if (CrucialDirectoriesExist())
            {
                if (!Directory.Exists(_settings.TorrentFilesDirectory))
                    Directory.CreateDirectory(_settings.TorrentFilesDirectory);

                textBox.WriteLine(">> Searching for currently airing anime episodes ...");
                var downloaded = await _downloader.DownloadAsync(_xml.Controller.AiringAnimes.ToList(), textBox);
                textBox.WriteLine(downloaded > 0
                    ? $">> Found {downloaded} anime downloads."
                    : ">> No new anime found.");
            }
        }

        /// <summary>
        ///     DownloadOutput (Get up to date)
        /// </summary>
        private async Task GetUpToDateAsync(TextBox textBox)
        {
            var response =
                MessageBox.Show(
                    "Don't do this often, it might use a lot of requests. You could also potentially download " +
                    "the wrong series if the intended series isn't found by your anime name and settings. \n\n" +
                    "Are you sure you want to?", 
                    "Confirmation", 
                    MessageBoxButton.YesNo);

            if (response == MessageBoxResult.Yes)
            {
                var total = 0;
                textBox.WriteLine(">> Attempting to catch up on airing anime episodes ...");
                foreach (var anime in _xml.Controller.AiringAnimes.ToList())
                {
                    bool downloaded;
                    do
                    {
                        downloaded = await _downloader.DownloadEpisodeAsync(await anime.GetLinksToNextEpisode(), anime, textBox);
                        if (downloaded)
                            total++;
                    } while (downloaded);
                }

                textBox.WriteLine(total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.");
            }

            else if (response == MessageBoxResult.No)
            {
                this.ToggleButtons();
                DownloadButton.Press();
                this.ToggleButtons();
            }
        }

        /// <summary>
        ///     DownloadOutput (Download missing episodes)
        /// </summary>
        /// <remarks>
        ///     Find and download any episodes in collection anime that are between the range
        ///     start.episode and last.episode
        /// </remarks>
        private async Task GetMissingEpisodesAsync(TextBox textBox)
        {
            textBox.WriteLine(">> Finding all missing episodes ...");
            var allEpisodeFiles = await Task.Run(() => _filehandler.AllAnimeEpisodes().ToList());
            var animeFileDeltas = await Task.Run(() =>
                _filehandler.FirstEpisodesOf(allEpisodeFiles)
                    .OrderBy(a => a.Name)
                    .Zip(
                        _filehandler.LastEpisodesOf(allEpisodeFiles).OrderBy(a => a.Name),
                        (a, b) => new AnimeEpisodeDelta(a, b))
                );
            var total = await _downloader.DownloadAsync(_allAnime.AiringAndWatching(),
                animeFileDeltas, allEpisodeFiles, textBox);

            textBox.WriteLine(total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.");
        }

        /* --Web */

        private void WebButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Web>();

            display.AnidbButton.Click += delegate { Process.Start("https://anidb.net/"); };

            display.AnichartButton.Click += delegate { Process.Start("http://anichart.net/"); };

            display.MyanimelistButton.Click += delegate { Process.Start("http://myanimelist.net/"); };

            display.NyaaButton.Click += delegate { Process.Start("https://www.nyaa.se/"); };

            display.SearchTextBox.KeyDown += (o, args) =>
            {
                if (args.Key == Key.Enter)
                    ((Web) CurrentDisplay).SearchButton.Press();
            };

            display.SearchButton.Click += delegate
            {
                var text = ((Web) CurrentDisplay).SearchTextBox.Text.Trim();
                if (text.Length > 0)
                {
                    var q = HttpUtility.ParseQueryString(text);
                    Process.Start($"http://myanimelist.net/anime.php?q={q}");
                }
            };

            display.FirstResultButton.Click += async delegate
            {
                var text = display.SearchTextBox.Text.Trim();
                if (text.Length > 0)
                {
                    await SearchOnNyaa(text);
                }
            };
        }

        /* --Misc */

        /// <summary>
        ///     View: Misc
        /// </summary>
        private void MiscButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Misc>();

            display.GetAll<RadioButton>().ForEach(r => r.KeyDown += (o, args) =>
            {
                if (args.Key == Key.Enter)
                    display.ButtonSubmit.Press();
            });

            display.ButtonSubmit.Click += Misc_ButtonMisc_Submit;
           
        }

        public async Task SearchOnNyaa(string text)
        {
            this.ToggleButtons();

            var q = HttpUtility.ParseQueryString(text);
            var document = new HtmlDocument();

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(new Uri($"http://myanimelist.net/anime.php?q={q}"));
                document.LoadHtml(html);
            }

            var link =
                document.DocumentNode?.SelectSingleNode(
                    "//div[@class=\"js-categories-seasonal js-block-list list\"]/table/tr[2]/td[1]")?
                    .Descendants("a")?
                    .FirstOrDefault();

            if (link != null)
            {
                Process.Start(link.Attributes["href"].Value);
            }
            else
            {
                Alert("No results found.");
            }

            this.ToggleButtons();
        }

        /// <summary>
        ///     Event: Submit -> Misc
        /// </summary>
        private async void Misc_ButtonMisc_Submit(object sender, RoutedEventArgs e)
        {
            this.ToggleButtons();

            var display = (Misc) CurrentDisplay;

            if (display.RadioDuplicates.IsChecked == true)
            {
                var count = await _filehandler.MoveDuplicatesAsync();
                Alert($"Moved {count} files to duplicate folder.");
            }
            else if (display.RadioIndexLastWatched.IsChecked == true)
            {
                _filehandler.AnimeEpisodesToLastEpisode_Watched(_allAnime.AiringAndWatching());
                Alert("Reset episode order to last known in Watched folder.");
            }
            else if (display.RadioIndexLastUnwatched.IsChecked == true)
            {
                _filehandler.AnimeEpisodesToLastEpisode_Unwatched(_allAnime.AiringAndWatching());
                Alert("Reset episode order to last known in any folder.");
            }
            else if (display.RadioIndexFirstWatched.IsChecked == true)
            {
                _filehandler.AnimeEpisodesToBeginningEpisode_All(_allAnime.AiringAndWatching());
                Alert("Reset episode count to first known episode.");
            }
            else if (display.RadioIndexZero.IsChecked == true)
            {
                foreach (var anime in _allAnime.AiringAndWatching())
                    anime.Episode = "00";
                Alert("Reset episode count to zero.");
            }

            this.ToggleButtons();
        }
    }
}