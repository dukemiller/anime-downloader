using System.Windows;
using System.Windows.Input;
using anime_downloader.Classes;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for DownloadOptions.xaml
    /// </summary>
    public partial class DownloadOptions
    {
        public DownloadOptions()
        {
            InitializeComponent();
        }

        private void Radio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton.Focus();
                SearchButton.Press();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SearchButton.Focus();
        }
    }
}