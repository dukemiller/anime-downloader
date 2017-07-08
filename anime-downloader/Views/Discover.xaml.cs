using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using anime_downloader.ViewModels;
using MessageBox = System.Windows.Forms.MessageBox;

namespace anime_downloader.Views
{
    /// <summary>
    /// Interaction logic for Discover.xaml
    /// </summary>
    public partial class Discover : UserControl
    {
        public Discover()
        {
            InitializeComponent();
        }

        private void Discover_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Dispatcher.BeginInvoke(new Action(() => MessageBox.Show($"{e.NewValue}, {e.OldValue}, {e.Property}")));
            (DataContext as DiscoverViewModel)?.VisibilityChanged((bool) e.NewValue);
        }
    }
}
