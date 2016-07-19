using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
using anime_downloader.Classes;
using anime_downloader.Classes.File;
using anime_downloader.Classes.Web;
using anime_downloader.Classes.Web.MyAnimeList;
using anime_downloader.Classes.Xml;
using anime_downloader.Views;
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
        private AnimeCollection _animeCollection;

        /// <summary>
        ///     The current display on the right window pane.
        /// </summary>
        public UserControl CurrentDisplay { get; private set; }

        /* Main window handlers  */

        public MainWindow()
        {
            if (AlreadyOpen())
                FocusOtherDownloaderAndClose();
            else
            {
                InitializeComponent();
                InitializeSettings();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (_settings != null)
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
            if (_settings != null)
            {
                if (_settings.Flags.ExitOnClose && !_tray.FullExit)
                    _tray.Visible = false;
                else if (_tray.FullExit)
                {
                    // exit is called through tray, no special handling
                }
                else
                {
                    WindowState = WindowState.Minimized;
                    e.Cancel = true;
                }
            }
        }

        /* Initializations */

        /// <summary>
        ///     Initialize and set the settings object.
        /// </summary>
        private void InitializeSettings()
        {
            if (!File.Exists(Classes.Settings.SettingsXml))
                Settings_CreateNew();
            else
            {
                _settings = new Classes.Settings(true);
                _animeCollection = new AnimeCollection(_settings);
                _filehandler = new FileHandler(_settings);
                _playlist = new Playlist(_filehandler);
                _downloader = new Downloader(_settings);
                _tray = new Tray(this, _settings);

                if (!Directory.Exists(Classes.Settings.ApplicationDirectory))
                    Directory.CreateDirectory(Classes.Settings.ApplicationDirectory);

                if (!File.Exists(Classes.Settings.AnimeXml))
                    Schema.CreateAnimeXml();

                InitialState();
            }
        }

        /// <summary>
        ///     The initial starting state after everything is successfully loaded.
        /// </summary>
        private void InitialState()
        {
            Verify.Schema(_settings);
            _tray.Initialize();
            _allAnime = _animeCollection.FilteredAndSorted().ToList();
            ChangeDisplay<Home>();

            KeyDown += (o, e) =>
            {
                // So you can type without changing the view
                if (Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is PasswordBox)
                    return;

                // Ctrl-X to close
                if (e.Key == Key.X && Keyboard.IsKeyDown(Key.LeftCtrl))
                    Close();

                // 1-8 to change views
                if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                    Cycle(HomeButton);
                else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                    Cycle(AnimeListButton);
                else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                    Cycle(SettingsButton);
                else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
                    Cycle(DownloadButton);
                else if (e.Key == Key.D5 || e.Key == Key.NumPad5)
                    Cycle(ManageButton);
                else if (e.Key == Key.D6 || e.Key == Key.NumPad6)
                    Cycle(PlaylistsButton);
                else if (e.Key == Key.D7 || e.Key == Key.NumPad7)
                    Cycle(WebButton);
                else if (e.Key == Key.D8 || e.Key == Key.NumPad8)
                    Cycle(MiscButton);
            };
        }

        /* Helper functions */

        /// <summary>
        ///     Returns the check if there is an already opened anime downloader.
        /// </summary>
        private static bool AlreadyOpen()
        {
            return Process
                .GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                .Length > 1;
        }

        /// <summary>
        ///     Focus the previously opened downloader and close the current.
        /// </summary>
        private void FocusOtherDownloaderAndClose()
        {
            const int restore = 9;
            var hwnd = FindWindow(null, "Anime Downloader");
            ShowWindow(hwnd, restore);
            SetForegroundWindow(hwnd);
            Close();
        }

        /// <summary>
        ///     To "refresh" views by rapidly cycling from home to ToggleButton button.
        /// </summary>
        private void Cycle(ToggleButton button)
        {
            HomeButton.Press();
            button.Press();
            button.IsChecked = true;
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
            if (CurrentDisplay != null && CurrentDisplay.GetType() == typeof(TView))
                return (TView) CurrentDisplay;
            CurrentDisplay = new TView();
            Display.Children.Clear();
            Display.Children.Add(CurrentDisplay);
            return (TView) CurrentDisplay;
        }

        /// <summary>
        ///     Completely clear focus from an element.
        /// </summary>
        private static void ClearFocusFrom(FrameworkElement element)
        {
            var parent = (FrameworkElement) element.Parent;
            while (parent != null && !((IInputElement) parent).Focusable)
                parent = (FrameworkElement) parent.Parent;
            var scope = FocusManager.GetFocusScope(element);
            FocusManager.SetFocusedElement(scope, parent);
        }

        /// <summary>
        ///     Throw an alert and return if there are any missing directories needed in the program.
        /// </summary>
        private bool CrucialDirectoriesExist()
        {
            var error = string.Empty;

            if (!Directory.Exists(_settings.Paths.EpisodeDirectory))
                error += "Your episode folder doesn't seem to exist.\n";

            if (!Directory.Exists(_settings.Paths.WatchedDirectory))
                error += "Your watched folder doesn't seem to exist.\n";

            if (!Directory.Exists(_settings.Paths.TorrentFilesDirectory))
                error += "Your torrent files folder doesn't seem to exist.\n";

            if (!File.Exists(_settings.Paths.UtorrentFile) || !_settings.Paths.UtorrentFile.ToLower().EndsWith(".exe"))
                error += "Your uTorrent.exe path seems to be wrong.";

            if (error.Length > 0)
                Alert(error);

            return error.Length == 0;
        }

        /// <summary>
        ///     Display an alert message (currently a messagebox).
        /// </summary>
        public static void Alert(string msg = "") => MessageBox.Show(msg);

        /// <summary>
        ///     Search for the first result of text on myanimelist and open in browser.
        /// </summary>
        private async Task SearchOnMyAnimeListAsync(string text)
        {
            this.ToggleButtons();

            var q = HttpUtility.UrlEncode(text);
            var document = new HtmlDocument();

            using (var client = new WebClient())
            {
                var html = await client.DownloadStringTaskAsync(new Uri($"http://myanimelist.net/anime.php?q={q}"));
                document.LoadHtml(html);
            }

            var link = document.DocumentNode?.SelectSingleNode(
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

            if (!File.Exists(Classes.Settings.PlaylistFile))
            {
                display.OpenButton.Toggle();
            }

            display.OpenButton.Click += delegate
            {
                if (File.Exists(Classes.Settings.PlaylistFile))
                    Process.Start(Classes.Settings.PlaylistFile);
            };
            display.CreateButton.Click += Playlist_PlaylistCreateButton_Click;
        }

        /// <summary>
        ///     Event: Submit -> Playlist
        /// </summary>
        private async void Playlist_PlaylistCreateButton_Click(object sender, RoutedEventArgs e)
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

                    await _playlist.Save();

                    Alert("Playlist created.");
                }

                Cycle(PlaylistsButton);
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

            // Clear any previous active event handlers
            KeyDown -= KeyEscapeBack;
            MouseDown -= MouseEscapeBack;

            // Add the dynamic data context
            display.Refresh(_animeCollection);
            display.FilterComboBox.Text = _settings.FilterBy;
            display.FilterComboBox.DropDownClosed += delegate
            {
                _settings.FilterBy = display.FilterComboBox.Text;
                display.Refresh(_animeCollection);
                CloseAnimeFindPopup();
            };

            // The event handlers
            display.Add.Click += AnimeList_Context_Add_Click;
            display.Edit.Click += AnimeList_Edit;
            display.Delete.Click += AnimeList_Context_Delete_Click;
            display.AddMultiple.Click += AnimeList_Context_AddMultiple_Click;
            display.Search.Click += AnimeList_Context_Search_Click;
            display.DataGrid.PreviewKeyDown += AnimeList_KeyDown;
            display.DataGrid.MouseDoubleClick += AnimeList_MouseDoubleClick;

            display.DataGrid.Sorting += (o, args) =>
            {
                var column = args.Column.Header.ToString().ToLower();
                _settings.SortBy = column.Equals("rating") ? "sortedrating" : column;
                _settings.Flags.SortByReversed = args.Column.SortDirection == ListSortDirection.Ascending;
            };

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

                if (keyEventArgs.Key == Key.F && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    CreateAnimeFindPopup();
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
                _animeCollection.Remove(cell.Item as Anime);
            CloseAnimeFindPopup();
            display.Refresh(_animeCollection);
        }

        /// <summary>
        ///     Event: AnimeList -> (Right-Click) Context -> Search : Click
        /// </summary>
        private async void AnimeList_Context_Search_Click(object sender, RoutedEventArgs e)
        {
            var display = (AnimeList) CurrentDisplay;
            var anime = display.DataGrid.SelectedCells.FirstOrDefault().Item as Anime;
            if (anime == null) return;

            if (anime.MyAnimeList.HasId)
                Process.Start($"http://myanimelist.net/anime/{anime.MyAnimeList.Id}");
            else
                await SearchOnMyAnimeListAsync(anime.Name);
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
                    _animeCollection.Remove(cell.Item as Anime);
                CloseAnimeFindPopup();
                display.Refresh(_animeCollection);
            }

            // Edit
            else if (e.Key == Key.Enter)
            {
                if (display.DataGrid.SelectedCells.FirstOrDefault().IsValid)
                {
                    AnimeList_Edit(sender, e);
                }
            }

            // Copy names to clipboard
            else if (e.Key == Key.OemComma && new[] { Key.LeftCtrl, Key.RightCtrl }.Any(Keyboard.IsKeyDown))
            {
                Clipboard.Clear();
                Clipboard.SetText(string.Join(", ", display.DataGrid.SelectedCells.Select(c => ((Anime) c.Item).Title).Distinct()));
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
                AnimeList_Edit(sender, e);
            }
        }

        /// <summary>
        ///     Event: Chooses view for Edit
        /// </summary>
        private void AnimeList_Edit(object sender, RoutedEventArgs e)
        {
            var tableDisplay = (AnimeList) CurrentDisplay;
            if (tableDisplay.DataGrid.SelectedCells.Count > 1)
                AnimeDetails_Multiple();
            else if (tableDisplay.DataGrid.SelectedCells.Count == 1)
                AnimeDetails_Single();
        }

        /// <summary>
        ///     Secondary View: Create anime find box
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
                Margin = new Thickness(270, 250, 0, 0),
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
                var result = _animeCollection.FilteredAndSorted().Where(a => a.Name.ToLower().Contains(q));
                display.DataGrid.ItemsSource = result;
            };

            Display.Children.Add(find);
            find.Focus();
        }

        /// <summary>
        ///     Secondary View: Remove anime find box
        /// </summary>
        private void CloseAnimeFindPopup()
        {
            var findWindow = Display.Children.OfType<TextBox>().FirstOrDefault(t => t.Name.Equals("FindBox"));

            if (findWindow != null)
                Display.Children.Remove(findWindow);

            var display = CurrentDisplay as AnimeList;

            if (display != null)
            {
                display.DataGrid.ItemsSource = _animeCollection.FilteredAndSorted();
                display.DataGrid.Focus();
            }
        }

        /* --AnimeDetails & AnimeDetailsMultiple */

        /// <summary>
        ///     Handler: Main window keypress returning back to anime list
        /// </summary>
        private void KeyEscapeBack(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)// || e.Key == Key.Back)
                Cycle(AnimeListButton);
        }

        /// <summary>
        ///     Handler: Main window mouse press returning back to anime list
        /// </summary>
        private void MouseEscapeBack(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton.Equals(MouseButton.XButton1))
                Cycle(AnimeListButton);
        }

        /// <summary>
        ///     View: AnimeDetails || AnimeList -> (Right-Click) Context -> Add : Click
        /// </summary>
        private void AnimeList_Context_Add_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<AnimeDetails>();

            // Press Escape or mouse back or backspace to go back
            KeyDown += KeyEscapeBack;
            MouseDown += MouseEscapeBack;

            // Default template
            display.DataContext = new Anime
            {
                Episode = "00",
                Status = "Watching",
                Resolution = "720",
                Airing = true
            };

            display.SubmitButton.Click += AnimeDetails_SubmitButton_Click;
            _settings.Subgroups.ToList().ForEach(s => display.SubgroupComboBox.Items.Add(s));
            display.OpenLastButton.Visibility = Visibility.Hidden;
        }

        /// <summary>
        ///     View: AnimeDetailsMultiple || AnimeList -> (Right-Click) Context -> Add Multiple : Click
        /// </summary>
        private void AnimeList_Context_AddMultiple_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<AnimeDetailsMultiple>();

            display.EpisodeTextBox.Text = "00";

            display.RatingTextBox.Toggle();

            display.InputTextBox.Loaded += delegate { ((AnimeDetailsMultiple) CurrentDisplay).InputTextBox.Focus(); };

            display.SubmitButton.Click += delegate
            {
                var names = display.InputTextBox.Text
                    .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.ToLower())
                    .ToList();
                if (names.Distinct().Count() != names.Count)
                    Alert("Names have to be unique.");
                else if (_allAnime.Select(a => a.Name.ToLower()).Intersect(names).Any())
                    Alert("A title entered already exists in the anime list.");
                else
                {
                    foreach (var name in names)
                    {
                        _animeCollection.Add(new Anime
                        {
                            Name = name,
                            Airing = display.AiringCheckBox.IsChecked ?? false,
                            Episode = $"{int.Parse(display.EpisodeTextBox.Text):D2}",
                            Status = display.StatusComboBox.Text,
                            Resolution = display.ResolutionComboBox.Text
                        });
                    }
                    AnimeListButton.Press();
                }
            };
        }

        /// <summary>
        ///     View: AnimeDetails || AnimeList -> (Right-Click) Context -> Edit
        /// </summary>
        public void AnimeDetails_Single(Anime anime = null)
        {
            if (anime == null)
            {
                var tableDisplay = (AnimeList) CurrentDisplay;
                anime = tableDisplay.DataGrid.SelectedCells.FirstOrDefault().Item as Anime;
            }

            if (anime == null)
                return;

            var display = ChangeDisplay<AnimeDetails>();
            display.DataContext = anime;

            display.SubmitButton.Content = "Edit";
            display.SubmitButton.Click += AnimeDetails_EditAnimeButton_Click;

            _currentlyEditedAnime = anime;

            // Press Escape or mouse back or backspace to go back
            KeyDown += KeyEscapeBack;
            MouseDown += MouseEscapeBack;

            _settings.Subgroups.ToList().ForEach(s => display.SubgroupComboBox.Items.Add(s));

            display.SubgroupComboBox.Text = anime.PreferredSubgroup != null && anime.PreferredSubgroup.Equals("")
                ? "(None)"
                : anime.PreferredSubgroup;

            display.OpenLastButton.Click += delegate
            {
                var episode = _filehandler.LastEpisodeOf(_currentlyEditedAnime);
                if (episode == null)
                    Alert($"Episode {_currentlyEditedAnime.Episode} for '{_currentlyEditedAnime.Name}' not found in any directory.");
                else
                    Process.Start($"{episode.FilePath}");
            };

            display.GotoMalButton.Click += delegate
            {
                Process.Start($"http://myanimelist.net/anime/{anime.MyAnimeList.Id}");
            };

            display.ClearMalButton.Click += delegate
            {
                anime.MyAnimeList.Id = "";
                anime.MyAnimeList.NeedsUpdating = true;
                anime.MyAnimeList.SeriesContinuationEpisode = "";
                anime.MyAnimeList.TotalEpisodes = "";
                anime.MyAnimeList.English = "";
                anime.MyAnimeList.Image = "";
                anime.MyAnimeList.Synopsis = "";
                anime.MyAnimeList.Title = "";
                anime.MyAnimeList.Synonyms = "";
                Alert("Cleared all MyAnimeList data about this show.");
                display.ClearMalButton.Visibility = Visibility.Hidden;
            };

            display.RefreshMalButton.Click += async delegate
            {
                this.ToggleButtons();
                var credentials = Api.GetCredentials(_settings);
                var animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(anime.Title));
                var result = animeResults.FirstOrDefault(r => r.Id.Equals(anime.MyAnimeList.Id));
                if (result != null)
                {
                    anime.MyAnimeList.Synopsis = result.Synopsis;
                    anime.MyAnimeList.Image = result.Image;
                    anime.MyAnimeList.Title = result.Title;
                    anime.MyAnimeList.English = result.English;
                    anime.MyAnimeList.Synopsis = result.Synopsis;
                    anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;
                    Alert("Updated any information about this show");
                }
                this.ToggleButtons();
            };

        }

        /// <summary>
        ///     View: AnimeDetailsMultiple || Anime list (edit multiple)
        /// </summary>
        private void AnimeDetails_Multiple()
        {
            var tableDisplay = (AnimeList) CurrentDisplay;
            var animes = tableDisplay.DataGrid.SelectedCells.Select(a => a.Item as Anime).Distinct().ToList();
            var display = ChangeDisplay<AnimeDetailsMultiple>();

            // Press Escape or mouse back or backspace to go back
            KeyDown += KeyEscapeBack;
            MouseDown += MouseEscapeBack;

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
            display.InputTextBox.Text = string.Join("\n", animes.Select(a => a.Title));
            display.InputTextBox.IsReadOnly = true;

            display.SubmitButton.Click += delegate
            {
                foreach (var anime in animes)
                {
                    if (!display.RatingTextBox.Text.IsBlank())
                        anime.Rating = display.RatingTextBox.Text;
                    if (!display.EpisodeTextBox.Text.IsBlank())
                        anime.Episode = display.EpisodeTextBox.Text;
                    anime.Status = display.StatusComboBox.Text;
                    anime.Airing = display.AiringCheckBox.IsChecked.Value;
                    anime.Resolution = display.ResolutionComboBox.Text;
                }
                AnimeListButton.Press();
            };
        }

        /// <summary>
        ///     Event: AnimeDetails -> Add : Click
        /// </summary>
        private void AnimeDetails_SubmitButton_Click(object sender, RoutedEventArgs e)
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

                _animeCollection.Add(new Anime
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
                _currentlyEditedAnime.PreferredSubgroup = subgroup.Equals("(None)") ? "" : subgroup;
                if (_currentlyEditedAnime.Status.Equals("Finished") && _currentlyEditedAnime.Airing)
                    _currentlyEditedAnime.Airing = false;
                Cycle(AnimeListButton);
            }
        }

        /* --Settings */

        /// <summary>
        ///     View: Settings (edit)
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Views.Settings>();
            display.SettingsGrid.DataContext = _settings;
            display.AlwaysTrayCheckbox.Click += delegate { _tray.CheckVisibility(); };
            display.ApplyChangesButton.Click += delegate { _settings.Save(); };
        }

        /// <summary>
        ///     View: Settings (new)
        /// </summary>
        private void Settings_CreateNew()
        {
            this.ToggleButtons();
            var display = ChangeDisplay<Views.Settings>();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "anime-downloader");

            // Default guessed values
            // TODO yeah you know, i don't really like how this works
            var settings = new Classes.Settings
            {
                Paths =
                {
                    EpisodeDirectory = Path.Combine(path, "Shows"),
                    WatchedDirectory = Path.Combine(path, "Watched"),
                    TorrentFilesDirectory = Path.Combine(path, "Torrents"),
                    UtorrentFile = @"C:\Program Files (x86)\uTorrent\uTorrent.exe"
                }
            };

            display.DataContext = settings;

            display.ApplyChangesButton.Content = "Create";

            display.ApplyChangesButton.Click += (obj, ev) =>
            {
                if (display.EpisodeTextbox.Empty() || display.TorrentTextbox.Empty() || display.UtorrentTextbox.Empty())
                    Alert("You must enter in the episode, torrent files and utorrent path information.");
                else
                {
                    this.ToggleButtons();
                    ((Classes.Settings) display.DataContext).Save();
                    InitializeSettings();
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
            display.LogButton.Click += DownloadOutput_LogHandlerAsync;
            display.SearchButton.Click += DownloadOutput_HandlerAsync;
        }

        /// <summary>
        ///     View: DownloadOutput
        /// </summary>
        private async void DownloadOutput_LogHandlerAsync(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<DownloadOutput>();

            var text = ">> No downloads have been logged so far.";

            if (File.Exists(Classes.Settings.LoggingFile))
            {
                using (var reader = new StreamReader(Classes.Settings.LoggingFile))
                    text = await reader.ReadToEndAsync();
                text = string.Join("\n", text.Split('\n').Reverse().Skip(1));
            }

            display.TextBox.Text = text;
        }

        /// <summary>
        ///     View: DownloadOutput
        /// </summary>
        private async void DownloadOutput_HandlerAsync(object sender, RoutedEventArgs e)
        {
            this.ToggleButtons();

            var display = CurrentDisplay as DownloadOptions;
            if (display == null)
            {
                this.ToggleButtons();
                return;
            }

            if (display.GetAll<RadioButton>().Any(r => r.IsChecked == true))
            {
                if (CrucialDirectoriesExist())
                {
                    var downloadDisplay = ChangeDisplay<DownloadOutput>();
                    var textBox = downloadDisplay.TextBox;
                    if (await Nyaa.IsOnlineAsync())
                    {
                        try
                        {
                            if (display.CheckForLatestRadio.IsChecked == true)
                                await DownloadOutput_CheckForLatestAsync(textBox);
                            else if (display.GetUpToDateRadio.IsChecked == true)
                                await DownloadOutput_GetUpToDateAsync(textBox);
                            else if (display.GetMissingRadio.IsChecked == true)
                                await DownloadOutput_GetMissingEpisodesAsync(textBox);
                        }
                        catch (Exception)
                        {
                            textBox.WriteLine($">> An error occured while attempting to download, try again."); 
                        }
                    }
                    else
                    {
                        textBox.WriteLine(">> Nyaa is currently offline. Try checking later.");
                    }
                    ClearFocusFrom(textBox);
                }
            }
            this.ToggleButtons();
        }

        /// <summary>
        ///     DownloadOutput (Check for latest anime)
        /// </summary>
        private async Task DownloadOutput_CheckForLatestAsync(TextBox textBox)
        {
            textBox.WriteLine(">> Searching for currently airing anime episodes ...");
            var downloaded = await _downloader.DownloadAsync(_animeCollection.AiringAndWatching.ToList(), textBox);
            textBox.WriteLine(downloaded > 0
                ? $">> Found {downloaded} anime downloads."
                : ">> No new anime found.");
        }

        /// <summary>
        ///     DownloadOutput (Get up to date)
        /// </summary>
        private async Task DownloadOutput_GetUpToDateAsync(TextBox textBox)
        {
            var response =
                MessageBox.Show(
                    "You could potentially download an entire wrong series if the intended series isn't " +
                    "found by your anime name and settings. Be sure everything on your list retrieves the " +
                    "show you intend. \n\n" +
                    "Are you sure you want to continue?",
                    "Confirmation",
                    MessageBoxButton.YesNo);

            if (response == MessageBoxResult.Yes)
            {
                var total = 0;
                textBox.WriteLine(">> Attempting to catch up on airing anime episodes ...");
                foreach (var anime in _animeCollection.AiringAndWatching.ToList())
                {
                    bool downloaded;
                    do
                    {
                        downloaded =
                            await _downloader.DownloadEpisodeAsync(await anime.GetLinksToNextEpisode(), anime, textBox);
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
        private async Task DownloadOutput_GetMissingEpisodesAsync(TextBox textBox)
        {
            textBox.WriteLine(">> Finding all missing episodes ...");
            var allEpisodeFiles = await Task.Run(() => _filehandler.Episodes(EpisodeType.All).ToList());
            var animeFileDeltas = await Task.Run(() =>
                FileHandler.FirstEpisodesOf(allEpisodeFiles).OrderBy(a => a.Name)
                    .Zip(
                    FileHandler.LastEpisodesOf(allEpisodeFiles).OrderBy(a => a.Name), (a, b) => new AnimeEpisodeDelta(a, b))
                );
            var total = await _downloader.DownloadAsync(_allAnime.AiringAndWatching(),
                animeFileDeltas, allEpisodeFiles, textBox);

            textBox.WriteLine(total > 0 ? $">> Found {total} anime downloads." : ">> No new anime found.");
        }

        /* --Manage */

        /// <summary>
        ///     View: Manage
        /// </summary>
        private async void ManageButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Manage>();
            display.Playlist = _playlist;
            display.SetInitialValues(
                this,
                await Task.Run(() => _filehandler.Episodes(EpisodeType.Unwatched)),
                await Task.Run(() => _filehandler.Episodes(EpisodeType.Watched)),
                _settings);
        }

        /* --Web */

        /// <summary>
        ///     View: Web
        /// </summary>
        private async void WebButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Web>();
            display.MyAnimeListGroupbox.DataContext = _settings.MyAnimeList;

            // TODO: figure out how to add this to the datacontext later
            var upToDate = await Task.Run(() => !_animeCollection.Animes.Any(a => a.MyAnimeList.NeedsUpdating && !a.Status.Equals("On Hold")));

            display.SyncedUp.Content = upToDate ? "✓" : "✗";
            display.SyncedUp.Foreground = upToDate ? Color.Green.ToBrush() : Color.Red.ToBrush();

            if (_settings.MyAnimeList.Works)
            {
                // Dont need to click sync if you're up to date
                display.SyncButton.IsHitTestVisible = !upToDate;
                display.SyncButton.Opacity = upToDate ? 0.6 : 1.0;
            }

            display.LoginButton.Click += async delegate
            {
                if (DateTime.Now < display.WaitDelay)
                    return;

                display.WaitDelay = DateTime.Now.AddSeconds(5);
                var credentials = Api.GetCredentials(_settings);
                var result = await Api.VerifyAsync(credentials);
                var temp = _settings.MyAnimeList.Works;
                _settings.MyAnimeList.Works = result;

                if (temp != _settings.MyAnimeList.Works)
                    Cycle(WebButton);
            };

            display.SearchButton.Click += delegate
            {
                var text = ((Web) CurrentDisplay).SearchTextBox.Text.Trim();
                if (text.Length > 0)
                {
                    var q = HttpUtility.UrlEncode(text);
                    Process.Start($"http://myanimelist.net/anime.php?q={q}");
                }
            };

            display.FirstResultButton.Click += async delegate
            {
                var text = display.SearchTextBox.Text.Trim();
                if (text.Length > 0)
                {
                    await SearchOnMyAnimeListAsync(text);
                }
            };

            display.UsageButton.Click += delegate
            {
                Alert("There are a few tricks and quirks to correctly use the synchronization: \n\n" +

                      "1. Be partial against using any nicknames for the show, you have a higher chance " +
                      "of finding the show with original english or romaji.\n\n" +

                      "2. OVAs have absolutely no chance of being found, so don't expect them to be " +
                      "found. Anime shorts can still be found if they're the content of the show, " +
                      "i.e. the show itself is only shorts.\n\n" +

                      "3. If the show has a close matching name to another series or is a single " +
                      "word (e.g. GATE vs Steins;Gate), flagging in the anime details for 'name " +
                      "strict' will find exact matches of the show and have a greater chance of " +
                      "correctly tagging the right show.\n\n" +

                      "4. For shows that have a season with another name, try your hardest to " +
                      "maintain that naming by adding a new series and marking the original " +
                      "series as complete instead of keeping the same name and downloading new " +
                      "episodes. It should still work, but it's bound to cause some type of " +
                      "problem."
                    );
            };

            display.ProfileButton.Click += delegate
            {
                Process.Start($"http://myanimelist.net/profile/{((Web)CurrentDisplay).UsernameTextbox.Text}");
            };

            display.SyncButton.Click += Web_SyncHandler;
        }

        private async void Web_SyncHandler(object sender, RoutedEventArgs e)
        {
            this.ToggleButtons();

            // Get credentials
            var credentials = Api.GetCredentials(_settings);

            // for every anime that needs updating
            foreach (var anime in _animeCollection.Animes.Where(a => a.MyAnimeList.NeedsUpdating && !a.Status.Equals("On Hold")))
            {
                // if no id is found
                if (anime.MyAnimeList.Id.IsBlank())
                {
                    // get all results from searching the name
                    var animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(anime.Title));

                    // if there were absolutely no results from the query
                    if (!animeResults.Any())
                    {
                        // Continually segment words and attempt to get a result
                        var name = anime.Title.Split(' ');
                        var length = name.Length;
                        while (!animeResults.Any() && length-- > 1)
                        {
                            animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(string.Join(" ", name.Take(length))));
                        }

                        // if after the previous operation there are still no results
                        if (!animeResults.Any())
                        {
                            // throw an error then skip
                            Alert($"1. Absolutely no matching names found for {anime.Title}.");
                            continue;
                        }
                    }

                    // make an estimation as to what is the closest result related to the anime
                    var result = anime.ClosestMyAnimeListResult(animeResults);

                    // if there was no good guess
                    if (result == null)
                    {
                        // try slapping a (TV) infront of it because the MAL api is weird sometimes
                        animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(anime.Title + " (TV)"));
                        result = anime.ClosestMyAnimeListResult(animeResults);

                        // if still no result
                        if (result == null)
                        {
                            // throw an error then skip
                            Alert($"2. No partial matches found from matching names for {anime.Title}.");
                            continue;
                        }
                    }

                    // check episode details if there is a given total (you can only hope)
                    if (result.IntTotalEpisodes() > 0)
                    {
                        // if you have downloaded more episodes than exists in the show, then you probably mislabeled
                        // this show as a s2 show but i'll go through painstaking effort to make it work anyway
                        if (anime.IntEpisode() > result.IntTotalEpisodes())
                        {
                            // track episode total
                            var total = result.IntTotalEpisodes();

                            // remove current series from list of possible choices
                            animeResults.Remove(result);
                            result = anime.ClosestMyAnimeListResult(animeResults);
                            total += result?.IntTotalEpisodes() ?? 0;

                            // if the combination of both this season is still less than your current episode
                            // you've probably mislabled this show for a few seasons dude, there's no way i can
                            // accurately guess which series is yours so i'll continue going through results until
                            // hopefully i can reach a point that it isnt
                            while (result != null && total < anime.IntEpisode())
                            {
                                animeResults.Remove(result);
                                result = anime.ClosestMyAnimeListResult(animeResults);
                                total += result?.IntTotalEpisodes() ?? 0;
                            }

                            // if we've run out of episodes, games over
                            if (result == null)
                            {
                                Alert($"3. Episode mismatch and no new series match for {anime.Title}.\n" +
                                    $"Given total: {total}, current episode: {anime.IntEpisode()}");
                                continue;
                            }

                            // keep track of episodes to update instead in this variable
                            anime.MyAnimeList.SeriesContinuationEpisode = $"{anime.IntEpisode() - total:D2}";
                            anime.MyAnimeList.OverallTotal = $"{total:D2}";
                        }
                        
                    }

                    // add all the details available
                    anime.MyAnimeList.Id = result.Id;
                    anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;
                    anime.MyAnimeList.Synopsis = result.Synopsis;
                    anime.MyAnimeList.Image = result.Image;
                    anime.MyAnimeList.Title = result.Title;
                    anime.MyAnimeList.English = result.English;
                    anime.MyAnimeList.Synonyms = result.Synonyms;

                    // TODO fix this for any season over S2
                    // convert to node for easier serialization, also checking if this is a mock second season
                    var myAnimeListNode = !anime.MyAnimeList.SeriesContinuationEpisode.IsBlank()
                        ? new Update(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                        : new Update(anime);

                    // add the data
                    await Api.AddAsync(credentials, anime.MyAnimeList.Id, myAnimeListNode.ToString());

                    // reset flag to update
                    anime.MyAnimeList.NeedsUpdating = false;
                }
                else
                {
                    // TODO fix this for any season over S2
                    // If this is a mock season or not
                    var myAnimeListNode = !anime.MyAnimeList.SeriesContinuationEpisode.IsBlank()
                        ? new Update(anime, anime.MyAnimeList.SeriesContinuationEpisode)
                        : new Update(anime);

                    // update the data
                    await Api.UpdateAsync(credentials, anime.MyAnimeList.Id, myAnimeListNode.ToString());

                    // reset flag to update
                    anime.MyAnimeList.NeedsUpdating = false;
                }
            }

            this.ToggleButtons();
            Cycle(WebButton);
        }

        /* --Misc */

        /// <summary>
        ///     View: Misc
        /// </summary>
        private void MiscButton_Click(object sender, RoutedEventArgs e)
        {
            var display = ChangeDisplay<Misc>();
            display.ButtonSubmit.Click += Misc_ButtonMisc_Submit;
        }

        /// <summary>
        ///     Event: Submit -> Misc
        /// </summary>
        private async void Misc_ButtonMisc_Submit(object sender, RoutedEventArgs e)
        {
            this.ToggleButtons();

            var display = (Misc) CurrentDisplay;

            if (display.DuplicatesRadio.IsChecked == true)
            {
                var count = await _filehandler.MoveDuplicatesAsync();
                Alert($"Moved {count} files to duplicate folder.");
            }

            else
            {
                var airingAnime = await Task.Run(() => _allAnime.AiringAndWatching().ToList());

                if (display.LastWatchedRadio.IsChecked == true)
                {
                    await _filehandler.SetToLastAsync(airingAnime, EpisodeType.Watched);
                    Alert("Reset episode order to last known in watched folder.");
                }

                else if (display.LastUnwatchedRadio.IsChecked == true)
                {
                    await _filehandler.SetToLastAsync(airingAnime, EpisodeType.Unwatched);
                    Alert("Reset episode order to last known in episode folder.");
                }

                else if (display.LastAnyRadio.IsChecked == true)
                {
                    await _filehandler.SetToLastAsync(airingAnime, EpisodeType.All);
                    Alert("Reset episode order to last known in any folder.");
                }

                else if (display.FirstWatchedRadio.IsChecked == true)
                {
                    await _filehandler.SetToFirstAsync(airingAnime, EpisodeType.All);
                    Alert("Reset episode count to first known episode.");
                }

                else if (display.ZeroRadio.IsChecked == true)
                {
                    foreach (var anime in airingAnime)
                        anime.Episode = "00";
                    Alert("Reset episode count to zero.");
                }

                else if (display.MarkCompleteRadio.IsChecked == true)
                {
                    var names = new List<string>();
                    foreach (var anime in airingAnime.Where(a => a.MyAnimeList.HasId && (
                        (a.MyAnimeList.IntOverallTotal() > 0 && a.IntEpisode() == a.MyAnimeList.IntOverallTotal()) ||
                        (a.MyAnimeList.IntTotalEpisodes() > 0 && a.IntEpisode() == a.MyAnimeList.IntTotalEpisodes())
                        )))
                    {
                        anime.Status = "Finished";
                        anime.Airing = false;
                        names.Add(anime.Title);
                    }

                    var result = names.Count > 0 ? string.Join(", ", names) : "no shows";
                    Alert($"Marked {result} as finished. ");
                }

                else if (display.SearchMoreRadio.IsChecked == true)
                {
                    var count = 0;

                    foreach (var anime in airingAnime
                        .Where(a => a.MyAnimeList.HasId && (a.MyAnimeList.IntTotalEpisodes() == 0)))
                    {

                        var credentials = Api.GetCredentials(_settings);
                        var animeResults = await Api.FindAsync(credentials, HttpUtility.UrlEncode(anime.Title));
                        var result = animeResults.FirstOrDefault(r => r.Id.Equals(anime.MyAnimeList.Id));

                        if (result != null)
                        {
                            anime.MyAnimeList.Synopsis = result.Synopsis;
                            anime.MyAnimeList.Image = result.Image;
                            anime.MyAnimeList.Title = result.Title;
                            anime.MyAnimeList.English = result.English;
                            anime.MyAnimeList.Synopsis = result.Synopsis;
                            anime.MyAnimeList.TotalEpisodes = result.TotalEpisodes;
                            count++;
                        }
                    }

                    Alert($"Attempted an update of {count} shows.");
                }
            }

            this.ToggleButtons();
        }
    }
}