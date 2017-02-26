using System.Diagnostics;
using System.IO;
using System.Reflection;
using static anime_downloader.Classes.OperatingSystemApi;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            if (AlreadyOpen)
            {
                FocusDownloader();
                Close();
            }

            else
                InitializeComponent();
        }
        
        /// <summary>
        ///     Returns the check if there is an already opened anime downloader.
        /// </summary>
        private static bool AlreadyOpen
        {
            get
            {
                var name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                return Process.GetProcessesByName(name).Length > 1;
            }
        }
    }
}