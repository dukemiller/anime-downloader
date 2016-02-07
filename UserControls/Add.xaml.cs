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

namespace anime_downloader.UserControls {
    /// <summary>
    /// Interaction logic for Add.xaml
    /// </summary>
    public partial class Add : UserControl {
        public Add() {
            InitializeComponent();
        }
       
        private void episode_textbox_GotFocus(object sender, RoutedEventArgs e) {
            episode_textbox.SelectAll();
        }

        private void name_textbox_GotFocus(object sender, RoutedEventArgs e) {
            name_textbox.SelectAll();
        }

        private void episode_textbox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }
        
    }
}
