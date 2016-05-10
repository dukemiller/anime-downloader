using anime_downloader.Views;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace anime_downloader.Classes
{
    public class Tray
    {
        private readonly MainWindow _mainWindow;

        private readonly Settings _settings;

        /// <summary>
        ///     The menu for the system tray.
        /// </summary>
        private ContextMenu _trayContextMenu;

        /// <summary>
        ///     The system tray.
        /// </summary>
        private NotifyIcon _trayIcon;

        public Tray(MainWindow mainWindow, Settings settings)
        {
            _mainWindow = mainWindow;
            _settings = settings;
        }

        public bool Visible
        {
            get { return _trayIcon.Visible; }

            set { _trayIcon.Visible = value; }
        }

        public void Initialize()
        {
            CreateTray();
            CreateContextMenu();
            if (_settings.AlwaysShowTray)
                Visible = true;
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

        private void CreateContextMenu()
        {
            _trayContextMenu = new ContextMenu();

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Download latest ...", (sender, args) =>
                {
                    if (_mainWindow.WindowState == WindowState.Minimized)
                    {
                        _mainWindow.Show();
                        _mainWindow.WindowState = WindowState.Normal;
                    }
                    _mainWindow.DownloadButton.Press();
                    ((DownloadOptions) _mainWindow.CurrentDisplay).SearchButton.Press();
                }));

            //
            _trayContextMenu.MenuItems.Add("-");
            // 

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Open base folder ...", (sender, args) =>
                {
                    if (_settings.Loaded)
                        Process.Start(_settings.BaseDirectory);
                }));

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Open settings folder ...", (sender, args) =>
                {
                    if (_settings.Loaded)
                        Process.Start(_settings.ApplicationDirectory);
                }));

            //
            _trayContextMenu.MenuItems.Add("-");
            // 

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Restore", (sender, args) =>
                {
                    _mainWindow.Show();
                    _mainWindow.WindowState = WindowState.Normal;
                }));

            _trayContextMenu.MenuItems.Add(
                new MenuItem("Exit", async (sender, args) =>
                {
                    Visible = false;
                    while (Visible)
                        await Task.Delay(100);
                    Application.Current.Shutdown();
                }));

            _trayIcon.ContextMenu = _trayContextMenu;
        }

        public void CheckVisibility()
        {
            if (_settings.AlwaysShowTray)
            {
                if (!Visible)
                    Visible = true;
            }

            else if (!_settings.AlwaysShowTray)
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
        }
    }
}