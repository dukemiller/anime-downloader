namespace anime_downloader.Views {
    /// <summary>
    /// Interaction logic for AnimeDetailsMultiple.xaml
    /// </summary>
    public partial class AnimeDetailsMultiple {
        public AnimeDetailsMultiple() {
            InitializeComponent();
        }

        private void EpisodeTextBox_GotFocus(object sender, System.Windows.RoutedEventArgs e) {
            EpisodeTextBox.SelectAll();
        }

        private void EpisodeTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }
    }
}
