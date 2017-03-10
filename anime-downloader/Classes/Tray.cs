using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using anime_downloader.Enums;
using anime_downloader.Models;
using anime_downloader.Models.Configurations;
using anime_downloader.Services.Interfaces;
using anime_downloader.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using static anime_downloader.Classes.OperatingSystemApi;
using Application = System.Windows.Application;

namespace anime_downloader.Classes
{
    public class Tray : ViewModelBase
    {
        private readonly ISettingsService _settings;

        /// <summary>
        ///     The menu for the system tray.
        /// </summary>
        private ContextMenu _trayContextMenu;

        /// <summary>
        ///     The system tray.
        /// </summary>
        private NotifyIcon _trayIcon;

        // Constructors

        public Tray(ISettingsService settings)
        {
            _settings = settings;
            InitTray();
            InitContextMenu();

            Visible = _settings.FlagConfig.AlwaysShowTray;
            _settings.FlagConfig.PropertyChanged += FlagChanged;
            MainWindow.Closing += WindowIsClosing;
            MainWindow.StateChanged += WindowStateChanged;
        }

        // Properties

        private static MainWindow MainWindow => (MainWindow) Application.Current.MainWindow;

        private bool FullExit { get; set; }

        private bool Visible
        {
            get { return _trayIcon.Visible; }
            set { _trayIcon.Visible = value; }
        }

        // Events

        private void FlagChanged(object sender, PropertyChangedEventArgs args)
        {
            if (_settings.FlagConfig.AlwaysShowTray)
            {
                Visible = true;
            }
            else
            {
                if (MainWindow.WindowState == WindowState.Minimized)
                    Visible = true;
                else if (MainWindow.WindowState == WindowState.Normal)
                    if (Visible)
                        Visible = false;
            }
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            // Necessary for bringing focus from another application
            switch (MainWindow.WindowState)
            {
                case WindowState.Normal:
                    Visible = _settings.FlagConfig.AlwaysShowTray;
                    MainWindow.Show();
                    break;
                case WindowState.Minimized:
                    Visible = true;
                    MainWindow.Hide();
                    break;
            }
        }

        private void WindowIsClosing(object sender, CancelEventArgs e)
        {
            if (_settings.FlagConfig.ExitOnClose && !FullExit)
                Visible = false;

            else if (FullExit)
            {
                // exit is called through tray, no special handling
            }

            else
            {
                Visible = true;
                MainWindow.WindowState = WindowState.Minimized;
                e.Cancel = true;
            }
        }

        // 

        private void InitTray()
        {
            // get the image from the program
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("anime_downloader.Resources.Icons.icon.ico");
            Debug.Assert(stream != null, "stream != null");

            _trayIcon = new NotifyIcon
            {
                Icon = new Icon(stream)
            };

            _trayIcon.MouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Left)
                    if (MainWindow.WindowState == WindowState.Minimized)
                    {
                        MainWindow.Show();
                        MainWindow.WindowState = WindowState.Normal;
                    }
                    else if (MainWindow.WindowState == WindowState.Normal)
                    {
                        MainWindow.WindowState = WindowState.Minimized;
                    }
            };

            stream.Close();
        }

        private static void BringWindowToFocus()
        {
            if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
            }
            FocusDownloader();
        }

        [NeedsUpdating]
        private void InitContextMenu()
        {
            _trayContextMenu = new ContextMenu();

            _trayContextMenu.MenuItems.Add(
                new MenuItem("&Download latest...", (sender, args) =>
                {
                    BringWindowToFocus();
                    MessengerInstance.Send(ViewDisplay.Download);
                    MessengerInstance.Send(new RadioModel<DownloadOption> { Data = DownloadOption.Next });
                }));

            _trayContextMenu.MenuItems.Add(
                new MenuItem("&Sync MyAnimeList...", (sender, args) =>
                {
                    BringWindowToFocus();
                    if (_settings.MyAnimeListConfig.LoggedIn)
                    {
                        MessengerInstance.Send(ViewDisplay.Web);
                        MessengerInstance.Send(new NotificationMessage("tray_sync"));
                    }
                }));

            _trayContextMenu.MenuItems.Add("-");

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Open &episode folder...", (sender, args) =>
                {
                    if (_settings != null && Directory.Exists(_settings.PathConfig.Unwatched))
                        Process.Start(_settings.PathConfig.Unwatched);
                }));

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Open &watched folder...", (sender, args) =>
                {
                    if (_settings != null && Directory.Exists(_settings.PathConfig.Watched))
                        Process.Start(_settings.PathConfig.Watched);
                }));

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Open &application folder...", (sender, args) =>
                {
                    if (_settings != null)
                        Process.Start(PathConfiguration.ApplicationDirectory);
                }));

            _trayContextMenu.MenuItems.Add("-");

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Check for &updates", (sender, args) =>
                {
                    BringWindowToFocus();
                    MessengerInstance.Send(ViewDisplay.Home);
                    MessengerInstance.Send(new NotificationMessage("check_for_updates"));
                }));

            _trayContextMenu.MenuItems.Add(
                new MenuItem("E&xit", (sender, args) =>
                {
                    _trayIcon.Visible = false;
                    FullExit = true;
                    Application.Current.Shutdown();
                }));

            _trayIcon.ContextMenu = _trayContextMenu;
        }
    }
}