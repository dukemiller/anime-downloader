using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using anime_downloader.Views;

namespace anime_downloader.Classes
{
    public class FindPopup
    {
        private readonly AnimeList _animeList;
        
        private TextBox _textBox;
        
        public FindPopup(AnimeList animeList)
        {
            _animeList = animeList;
        }

        private TextBox CreateTextBox()
        {
            var textbox = new TextBox
            {
                Name = "FindBox",
                Width = 400,
                Height = 30,
                Margin = new Thickness(270, 250, 0, 0),
                FontSize = 18,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            textbox.KeyUp += delegate
            {
                var q = textbox.Text.ToLower().Trim();
                var result = MainWindow.Window.AnimeCollection.FilteredAndSorted.Where(a => a.Name.ToLower().Contains(q));
                _animeList.DataGrid.ItemsSource = result;
            };

            MainWindow.Window.AnimeList.Click += CloseOnRepeatedView;

            return textbox;
        }

        private void CloseOnRepeatedView(object sender, RoutedEventArgs routedEventArgs)
        {
            Close();
            MainWindow.Window.AnimeList.Click -= CloseOnRepeatedView;
        }

        private void Create()
        {
            if (_textBox != null)
                return;

            _textBox = CreateTextBox();
            MainWindow.Window.Display.Children.Add(_textBox);

            _animeList.DataGrid.MouseDoubleClick += FindDoubleClick;
            MainWindow.Window.KeyDown += FindKeyDown;

            _textBox.Focus();
        }

        private void FindKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
                Close();

            else if (args.Key == Key.F && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (_textBox != null)
                {
                    if (_textBox.IsSelectionActive)
                        Close();
                    else
                        _textBox.Focus();
                }
            }
        }

        private void FindDoubleClick(object sender, MouseButtonEventArgs args)
        {
            Close();
        }
        
        public void Close()
        {
            if (_textBox != null)
                MainWindow.Window.Display.Children.Remove(_textBox);

            _animeList.DataGrid.MouseDoubleClick -= FindDoubleClick;
            MainWindow.Window.KeyDown -= FindKeyDown;

            _textBox = null;
            _animeList.DataGrid.ItemsSource = MainWindow.Window.AnimeCollection.FilteredAndSorted;
            _animeList.DataGrid.Focus();
        }

        public void Toggle()
        {
            if (_textBox == null)
                Create();
            else
                Close();
        }
    }
}
