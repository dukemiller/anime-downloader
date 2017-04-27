using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace anime_downloader.ViewModels.Components
{
    public class FindViewModel : ViewModelBase
    {
        private string _text;

        private Visibility _visible;

        // 

        public FindViewModel()
        {
            Visible = Visibility.Collapsed;
            ClearCommand = new RelayCommand(() =>
            {
                if (Text?.Length > 0)
                    Text = string.Empty;
                else
                    Close();
            });
        }

        // 

        public string Text
        {
            get => _text;
            set => Set(() => Text, ref _text, value);
        }

        public Visibility Visible
        {
            get => _visible;
            set => Set(() => Visible, ref _visible, value);
        }

        public RelayCommand ClearCommand { get; set; }

        // 

        public void Toggle() => Visible = Visible == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

        public void Close()
        {
            Visible = Visibility.Collapsed;
            Text = "";
        }
    }
}