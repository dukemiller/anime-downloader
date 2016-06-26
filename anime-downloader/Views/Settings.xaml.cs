using anime_downloader.Classes;
using System.Windows.Input;

namespace anime_downloader.Views
{
    /// <summary>
    ///     Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Textbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyChangesButton.Press();
            }
        }
    }
}