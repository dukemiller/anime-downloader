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
    }
}