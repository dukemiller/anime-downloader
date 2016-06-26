using anime_downloader.Classes;
using System;
using System.Diagnostics;
using System.Web;
using System.Windows;
using System.Windows.Input;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Web.xaml
    /// </summary>
    public partial class Web
    {
        public DateTime WaitDelay { get; set; } = DateTime.Now;

        public Web()
        {
            InitializeComponent();
        }

        private void MyanimelistButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://myanimelist.net/");
        }

        private void AnichartButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://anichart.net/");
        }

        private void NyaaButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.nyaa.se/");
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchButton.Press();
        }

        private void SearchButton_KeyDown(object sender, KeyEventArgs e)
        {
            var text = SearchTextBox.Text.Trim();
            if (text.Length > 0)
            {
                var q = HttpUtility.ParseQueryString(text);
                Process.Start($"http://myanimelist.net/anime.php?q={q}");
            }
        }
    }
}