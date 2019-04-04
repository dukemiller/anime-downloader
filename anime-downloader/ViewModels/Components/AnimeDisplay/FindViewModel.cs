using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components.AnimeDisplay
{
    public class FindViewModel : ViewModelBase
    {
        public string Text { get; set; } = "";

        public Visibility Visible { get; set; } = Visibility.Collapsed;

        public RelayCommand ClearCommand => new RelayCommand(Clear);

        // 

        public void Toggle() => Visible = Visible == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

        public void Close()
        {
            Visible = Visibility.Collapsed;
            Text = "";
        }

        public void Clear()
        {
            if (Text?.Length > 0)
                Text = string.Empty;
            else
                Close();
        }
    }
}