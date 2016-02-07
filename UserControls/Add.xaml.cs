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
        
        private void episode_textbox_KeyDown(object sender, KeyEventArgs e) {

            switch (e.Key) {
                case Key.D1: case Key.D2: case Key.D3: case Key.D4:
                case Key.D5: case Key.D6: case Key.D7: case Key.D8:
                case Key.D9: case Key.D0: case Key.NumPad0:
                case Key.NumPad1: case Key.NumPad2: case Key.NumPad3:
                case Key.NumPad4: case Key.NumPad5: case Key.NumPad6:
                case Key.NumPad7: case Key.NumPad8: case Key.NumPad9:
                    break;
                default:
                    e.Handled = true;
                    break;
            }
        }
        
    }
}
