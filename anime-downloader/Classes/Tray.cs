using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace anime_downloader.Classes
{
    public class Tray
    {
        /// <summary>
        ///     The menu for the system tray.
        /// </summary>
        private ContextMenu _trayContextMenu;

        /// <summary>
        ///     The system tray.
        /// </summary>
        private NotifyIcon _trayIcon;

        private readonly MainWindow _mainWindow;

        private readonly Settings _settings;

        public bool Visible
        {
            get { return _trayIcon.Visible; }

            set { _trayIcon.Visible = value; }
        }

        public Tray(MainWindow mainWindow, Settings settings)
        {
            _mainWindow = mainWindow;
            _settings = settings;
        }

        private void CreateTray()
        {
            // get the image from the program
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("anime_downloader.Resources.Icons.icon.ico");
            Debug.Assert(stream != null, "stream != null");
            // ../Resources/Images/find.png

            _trayIcon = new NotifyIcon
            {
                Icon = new Icon(stream)
            };

            _trayIcon.Click += delegate
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
            };

            // stream.Close();
        }

        private void CreateContextMenu()
        {
            _trayContextMenu = new ContextMenu();

            _trayContextMenu.MenuItems.Add(0,
                new MenuItem("Open base folder", (sender, args) =>
                {
                    if (_settings.Loaded)
                        Process.Start(_settings.BaseDirectory);
                }));

            _trayContextMenu.MenuItems.Add(1,
                new MenuItem("Restore", (sender, args) =>
                {
                    _mainWindow.Show();
                    _mainWindow.WindowState = WindowState.Normal;
                }));

            _trayContextMenu.MenuItems.Add(2,
                new MenuItem("Exit", (sender, args) =>
                {
                    _trayIcon.Visible = false;
                    System.Windows.Application.Current.Shutdown();
                }));

            _trayIcon.ContextMenu = _trayContextMenu;
        }

        /// <summary>
        ///     Create the tray and tray context menu.
        /// </summary>
        public void InitializeSystemTray()
        {
            CreateTray();
            CreateContextMenu();
            if (_settings.AlwaysShowTray)
                _trayIcon.Visible = true;
        }

        public void CheckVisibility()
        {
            if (_settings.AlwaysShowTray && !Visible)
                Visible = true;
            if (!_settings.AlwaysShowTray && Visible)
                Visible = false;
        }

        public void CheckAlwaysVisibility()
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                Visible = true;
            }

            else if (_mainWindow.WindowState == WindowState.Normal)
            {
                if (!_settings.AlwaysShowTray)
                {
                    Visible = false;
                }
            }
        }

    }
}