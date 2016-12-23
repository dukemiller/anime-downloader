using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using anime_downloader.Services;
using anime_downloader.Services.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Application = System.Windows.Application;

namespace anime_downloader.Classes
{
    public class Tray: ViewModelBase
    {
        private readonly Views.MainWindow _mainWindow;

        private readonly ISettingsService _settings;

        /// <summary>
        ///     The menu for the system tray.
        /// </summary>
        private ContextMenu _trayContextMenu;

        /// <summary>
        ///     The system tray.
        /// </summary>
        private NotifyIcon _trayIcon;

        public Tray(Views.MainWindow mainWindow, ISettingsService settings)
        {
            _mainWindow = mainWindow;
            _settings = settings;
            CreateTray();
            CreateContextMenu();
            if (_settings.FlagConfig.AlwaysShowTray)
                Visible = true;

            _settings.FlagConfig.PropertyChanged += (sender, args) =>
            {
                if (_settings.FlagConfig.AlwaysShowTray)
                {
                    if (!Visible)
                        Visible = true;
                }

                if (_settings.FlagConfig.AlwaysShowTray)
                {
                    if (!Visible)
                        Visible = true;
                }

                else if (!_settings.FlagConfig.AlwaysShowTray)
                {
                    if (_mainWindow.WindowState == WindowState.Minimized)
                    {
                        Visible = true;
                    }
                    else if (_mainWindow.WindowState == WindowState.Normal)
                    {
                        if (Visible)
                        {
                            Visible = false;
                        }
                    }
                }
            };
        }

        public bool FullExit { get; private set; }

        public bool Visible
        {
            get { return _trayIcon.Visible; }
            set { _trayIcon.Visible = value; }
        }

        private void CreateTray()
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
                {
                    if (_mainWindow.WindowState == WindowState.Minimized)
                    {
                        _mainWindow.Show();
                        _mainWindow.WindowState = WindowState.Normal;
                    }
                    else if (_mainWindow.WindowState == WindowState.Normal)
                    {
                        _mainWindow.WindowState = WindowState.Minimized;
                    }
                }
            };

            stream.Close();
        }

        private void BringWindowToFocus()
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
            }
        }

        [NeedsUpdating]
        private void CreateContextMenu()
        {
            _trayContextMenu = new ContextMenu();

            _trayContextMenu.MenuItems.Add(
                new MenuItem("&Download latest...", (sender, args) =>
                {
                    BringWindowToFocus();
                    MessengerInstance.Send(Enums.Views.Download);
                    MessengerInstance.Send("tray_download");
                }));

            _trayContextMenu.MenuItems.Add(
                new MenuItem("&Sync MyAnimeList...", (sender, args) =>
                {
                    BringWindowToFocus();
                    if (_settings.MyAnimeListConfig.Works)
                    {
                        MessengerInstance.Send(Enums.Views.Web);
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
                        Process.Start(_settings.PathConfig.ApplicationDirectory);
                }));

            _trayContextMenu.MenuItems.Add("-");

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