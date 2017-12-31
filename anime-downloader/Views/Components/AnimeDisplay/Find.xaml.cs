using System.Windows;

namespace anime_downloader.Views.Components.AnimeDisplay
{
    /// <summary>
    ///     Interaction logic for FindViewModel.xaml
    /// </summary>
    public partial class Find
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