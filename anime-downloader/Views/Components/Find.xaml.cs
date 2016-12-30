using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace anime_downloader.Views.Components
{
    /// <summary>
    /// Interaction logic for FindViewModel.xaml
    /// </summary>
    public partial class Find : UserControl
    {
        public Find()
        {
            InitializeComponent();
        }

        private void UIElement_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded && IsVisible)
                Textbox.Focus();
        }
    }
}
