using System.Linq;
using System.Windows;
using System.Windows.Controls;
using anime_downloader.Classes;
using System.Windows.Input;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Misc.xaml
    /// </summary>
    public partial class Misc
    {
        public Misc()
        {
            InitializeComponent();
        }

        private void Radio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ButtonSubmit.Press();
        }

        private async void ButtonSubmit_OnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.Window.ToggleButtons();
            var command = this.GetAll<RadioButton>().FirstOrDefault(r => r.IsChecked == true)?.Name;
            if (command != null)
                await MainWindow.Window.EpisodeHandler.HandleCommand(command);
            MainWindow.Window.ToggleButtons();
        }
    }
}