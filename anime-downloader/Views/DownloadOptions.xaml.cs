using anime_downloader.Classes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void UserControl_Loaded(object sender, RoutedEventArgs e) => SearchButton.Focus();

        private void LogButton_OnClick(object sender, RoutedEventArgs e)
            => MainWindow.Window.ChangeDisplay<Downloader>().Logger();

        private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach(var radio in this.GetAll<RadioButton>())
                if (radio.IsChecked == true)
                    MainWindow.Window.ChangeDisplay<Downloader>().Download(radio.Name);
        }
    }
}