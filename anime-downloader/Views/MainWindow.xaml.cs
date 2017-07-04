using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using anime_downloader.ViewModels;
using MahApps.Metro.Controls;
using static anime_downloader.Classes.OperatingSystemApi;
using MessageBox = System.Windows.Forms.MessageBox;

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

        private void RefreshView(object sender, MouseButtonEventArgs e)
        {
            // Only refresh view if i'm over the header tab item
            if (e.Source.GetType() == typeof(MetroTabItem))
                 (DataContext as MainWindowViewModel)?.RefreshView();
        }
    }
}