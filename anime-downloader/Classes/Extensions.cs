using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using anime_downloader.Classes.Xml;
using anime_downloader.Views;

namespace anime_downloader.Classes
{
    public static class Extensions
    {

        public static string CommaJoined(this string[] array)
        {
            return string.Join(", ", array);
        }

        public static IEnumerable<Anime> AiringAndWatching(this IEnumerable<Anime> animes)
        {
            return animes.Where(a => a.Airing && a.Status.Equals("Watching"));
        }

        // TODO: Hey turn this into a real data binding instead of a fake one
        /// <summary>
        ///     "Refresh" the datacontext on the UI.
        /// </summary>
        public static void Refresh(this AnimeList animeList, AnimeCollection collection)
        {
            var anime = collection.Animes.ToList();
            animeList.DataGrid.ItemsSource = collection.FilteredAndSorted();
            animeList.StatsLabel.Content = $"{anime.Count} total animes. " +
                                           $"{anime.Count(a => a.Airing && a.Status.Equals("Watching"))} airing/watching, " +
                                           $"{anime.Count(a => a.Status.Equals("Finished"))} finished, " +
                                           $"{anime.Count(a => a.Status.Equals("On Hold"))} on hold, " + 
                                           $"{anime.Count(a => a.Status.Equals("Dropped"))} dropped.";
        }

        /// <summary>
        ///     Check if the container is empty.
        /// </summary>
        public static bool Empty(this TextBox textbox) => textbox.Text.Equals("");

        public static void WriteLine(this TextBox textbox, string text)
        {
            textbox.AppendText(text + "\n");
            textbox.Focus();
            textbox.CaretIndex = textbox.Text.Length;
            textbox.ScrollToEnd();
        }

        /// <summary>
        ///     Simulate a button press on a control.
        /// </summary>
        public static void Press(this Control control)
        {
            if (control.IsHitTestVisible)
                control.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        /// <summary>
        ///     Toggle opacity and visibility of a Control between two states.
        /// </summary>
        public static void Toggle(this Control control)
        {
            control.Opacity = control.IsHitTestVisible ? 0.4 : 1.0;
            control.IsHitTestVisible ^= true;
        }

        /// <summary>
        ///     Toggle opacity and visibility of all buttons inside the MainWindow.
        /// </summary>
        public static void ToggleButtons(this MainWindow window)
        {
            foreach (var button in window.GetAll<ToggleButton>()) //.Union(window.GetAll<Button>(window)))
                 button.Toggle();
            foreach (var button in window.GetAll<Button>()) //.Union(window.GetAll<Button>(window)))
                button.Toggle();
        }

        public static void AddSorted<T>(this IList<T> list, T item, IComparer<T> comparer = null)
        {
            if (comparer == null)
                comparer = Comparer<T>.Default;

            int i = 0;
            while (i < list.Count && comparer.Compare(list[i], item) < 0)
                i++;

            list.Insert(i, item);
        }
        
        // http://stackoverflow.com/a/33523743
        public static System.Windows.Media.Brush ToBrush(this Color color)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public static List<T> GetAll<T>(this object parent) where T : DependencyObject
        {
            var logicalCollection = new List<T>();
            GetAll(parent as DependencyObject, logicalCollection);
            return logicalCollection;
        }

        private static void GetAll<T>(DependencyObject parent, ICollection<T> collection)
            where T : DependencyObject
        {
            var children = LogicalTreeHelper.GetChildren(parent);
            foreach (var child in children)
            {
                if (!(child is DependencyObject))
                    continue;
                var dependencyObject = child as DependencyObject;
                if (child is T)
                    collection.Add(child as T);
                GetAll(dependencyObject, collection);
            }
        }

        // http://stackoverflow.com/a/14591148
        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }

        public static string OnlyLettersAndSpace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsLetter(c) || !char.IsWhiteSpace(c))
                .ToArray());
        }

        public static bool IsBlank(this string str)
        {
            return str.Equals("");
        }
    }
}