using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using anime_downloader.Classes;
using anime_downloader.Classes.File;
using anime_downloader.Classes.Xml;
using anime_downloader.Models;
using anime_downloader.ViewModels;
using anime_downloader.Views;
using static anime_downloader.Classes.OperatingSystemApi;
using Downloader = anime_downloader.Classes.File.Downloader;
using Settings = anime_downloader.Models.Settings;

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
        public IEnumerable<Anime> AllAnime;
        
        /// <summary>
        ///     Handles downloading operations.
        /// </summary>
        public Downloader Downloader;

        /// <summary>
        ///     Handles tracking/managing files.
        /// </summary>
        public EpisodeHandler EpisodeHandler;

        /// <summary>
        ///     Handle playlist creation with some customization.
        /// </summary>
        public Playlist Playlist;

        /// <summary>
        ///     Handles paths and user settings.
        /// </summary>
        public Settings Settings;

        /// <summary>
        ///     Handles logic related to creating and the features of the system tray.
        /// </summary>
        public Tray Tray;

        /// <summary>
        ///     Handles objects for modifying and creating the xml files
        /// </summary>
        public AnimeCollection AnimeCollection;

        public AnimeFileCollection AnimeFileCollection;

        /// <summary>
        ///     The current display on the right window pane.
        /// </summary>
        public UserControl CurrentDisplay { get; private set; }

        /// <summary>
        ///     The filthiest way i've seen of globally accessing this instance, better hope
        ///     that there is never any other "MainWindow"
        /// </summary>
        public static MainWindow Window => (MainWindow) Application.Current.MainWindow;
        
        // 

        public MainWindow()
        {
            DataContext = new MainWindowViewModel(Close);

            if (AlreadyOpen)
                FocusOtherDownloaderAndClose();
            else
            {
                InitializeComponent();
                InitializeSettings();
            }
        }
        
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (Settings == null)
                return;

            // Necessary for bringing focus from another application
            switch (WindowState)
            {
                case WindowState.Normal:
                    Show();
                    break;
                case WindowState.Minimized:
                    Hide();
                    break;
            }

            Tray.CheckVisibility();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (Settings == null)
                return;

            if (Settings.Flags.ExitOnClose && !Tray.FullExit)
                Tray.Visible = false;

            else if (Tray.FullExit)
            {
                // exit is called through tray, no special handling
            }

            else
            {
                WindowState = WindowState.Minimized;
                e.Cancel = true;
            }
        }

        // 

        private void Home_Click(object sender, RoutedEventArgs e) => ChangeDisplay<Home>();

        private void AnimeList_Click(object sender, RoutedEventArgs e) => ChangeDisplay<AnimeList>();

        private void Settings_Click(object sender, RoutedEventArgs e) => ChangeDisplay<Views.Settings>().Load(Settings);

        private void Download_Click(object sender, RoutedEventArgs e) => ChangeDisplay<DownloadOptions>();

        private void Manage_Click(object sender, RoutedEventArgs e) => ChangeDisplay<Manage>();

        private void Playlists_Click(object sender, RoutedEventArgs e) => ChangeDisplay<PlaylistCreator>();

        private void Web_Click(object sender, RoutedEventArgs e) => ChangeDisplay<Web>();

        private void Misc_Click(object sender, RoutedEventArgs e) => ChangeDisplay<Misc>();

        // Initializations

        /// <summary>
        ///     Initialize and set the settings object.
        /// </summary>
        public void InitializeSettings()
        {
            if (!Directory.Exists(Settings.ApplicationDirectory))
                Directory.CreateDirectory(Settings.ApplicationDirectory);

            if (!File.Exists(Settings.SettingsXml))
                ChangeDisplay<Views.Settings>().New();

            else
            {
                if (!File.Exists(Settings.AnimeXml))
                    Schema.CreateAnimeXml();

                Settings = new Settings(true);
                AnimeCollection = new AnimeCollection(Settings);
                AnimeFileCollection = new AnimeFileCollection(Settings);
                EpisodeHandler = new EpisodeHandler(Settings);
                Playlist = new Playlist(AnimeFileCollection);
                Downloader = new Downloader(Settings);
                Tray = new Tray(this, Settings);

                InitialState();
            }
        }

        /// <summary>
        ///     The initial starting state after everything is successfully loaded.
        /// </summary>
        private void InitialState()
        {
            Verify.Schema(Settings);
            Tray.Initialize();
            AllAnime = AnimeCollection.FilteredAndSorted;
            ChangeDisplay<Home>();

            KeyDown += (o, e) =>
            {
                // So you can type without changing the view
                if (Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is PasswordBox)
                    return;

                /*
                // 1-8 to change views
                if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                    Cycle(Home);
                else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                    Cycle(AnimeList);
                else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                    Cycle(SettingsButton);
                else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
                    Cycle(Download);
                else if (e.Key == Key.D5 || e.Key == Key.NumPad5)
                    Cycle(Manage);
                else if (e.Key == Key.D6 || e.Key == Key.NumPad6)
                    Cycle(Playlists);
                else if (e.Key == Key.D7 || e.Key == Key.NumPad7)
                    Cycle(Web);
                else if (e.Key == Key.D8 || e.Key == Key.NumPad8)
                    Cycle(Misc);
                    */
            };
        }

        // Helper 

        /// <summary>
        ///     Returns the check if there is an already opened anime downloader.
        /// </summary>
        private static bool AlreadyOpen
        {
            get
            {
                return Process
                           .GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                           .Length > 1;
            }
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
        public void Cycle(ToggleButton button)
        {
            Home.Press();
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
        public TView ChangeDisplay<TView>() where TView : UserControl, new()
        {
            // Don't reload the same view
            if (CurrentDisplay != null && CurrentDisplay.GetType() == typeof(TView))
                return (TView) CurrentDisplay;
            CurrentDisplay = new TView();
            Display.Children.Clear();
            Display.Children.Add(CurrentDisplay);
            DisplayTransition();
            return (TView) CurrentDisplay;
        }

        public void DisplayTransition() => Display.BeginStoryboard((Storyboard)FindResource("DisplayTransition"));

        /// <summary>
        ///     Throw an alert and return if there are any missing directories needed in the program.
        /// </summary>
        public bool CrucialDirectoriesExist()
        {
            var error = string.Empty;

            if (!Directory.Exists(Settings.Paths.EpisodeDirectory))
                error += "Your episode folder doesn't seem to exist.\n";

            if (!Directory.Exists(Settings.Paths.WatchedDirectory))
                error += "Your watched folder doesn't seem to exist.\n";

            if (!Directory.Exists(Settings.Paths.TorrentFilesDirectory))
                error += "Your torrent files folder doesn't seem to exist.\n";

            if (!File.Exists(Settings.Paths.UtorrentFile) || !Settings.Paths.UtorrentFile.ToLower().EndsWith(".exe"))
                error += "Your uTorrent.exe path seems to be wrong.";

            if (error.Length > 0)
                Methods.Alert(error);

            return error.Length == 0;
        }
    }
}