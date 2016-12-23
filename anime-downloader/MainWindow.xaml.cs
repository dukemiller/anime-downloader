using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;
using anime_downloader.Classes;
using anime_downloader.Services;
using anime_downloader.ViewModels;
using anime_downloader.Views;
using static anime_downloader.Classes.OperatingSystemApi;

namespace anime_downloader
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private IAnimeAggregateService AnimeAggregate { get; }

        private ISettingsService Settings { get; }

        /// <summary>
        ///     Handles logic related to creating and the features of the system tray.
        /// </summary>
        private readonly Tray _tray;
        
        // 

        public MainWindow()
        {
            Settings = new SettingsService();
            AnimeAggregate = new AnimeAggregateService(Settings);
            DataContext = new MainWindowViewModel(Settings, AnimeAggregate, Close);

            if (AlreadyOpen)
                FocusOtherDownloaderAndClose();
            else
            {
                _tray = new Tray(this, Settings);
                InitializeComponent();
            }
        }

        // 
        
        private void Window_StateChanged(object sender, EventArgs e)
        {
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
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (Settings.FlagConfig.ExitOnClose && !_tray.FullExit)
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

        // public void DisplayTransition() => Display.BeginStoryboard((Storyboard)FindResource("DisplayTransition"));

    }
}