using System.Collections.Generic;
using System.ComponentModel;
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
        /// <summary>
        ///     Sort the animes with the specified property name of the anime type
        /// </summary>
        /// <param name="animes"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        /// <remarks>
        ///     The sort has to be a property of Anime, or else this will fail. This was the
        ///     only way I could dynamically change the property of the sort
        /// </remarks>
        public static IEnumerable<Anime> SortedWith(this IEnumerable<Anime> animes, string sort)
        {
            var prop = TypeDescriptor.GetProperties(typeof (Anime)).Find(sort, true);
            return animes.OrderBy(x => prop.GetValue(x));
        }

        public static IEnumerable<Anime> Watching(this IEnumerable<Anime> animes)
        {
            return animes.Where(a => a.Status.Equals("Watching"));
        }

        public static IEnumerable<Anime> Airing(this IEnumerable<Anime> animes)
        {
            return animes.Where(a => a.Airing && a.Status == "Watching");
        }

        public static Anime Get(this IEnumerable<Anime> animes, string name)
        {
            return name.Split(' ').Length == 0
                ? animes.FirstOrDefault(anime => anime.Name.ToLower().Equals(name.ToLower()))
                : animes.FirstOrDefault(anime => anime.Name.ToLower().Contains(name.ToLower()));
        }

        // TODO: Hey turn this into a real data binding instead of a fake one
        /// <summary>
        ///     "Refresh" the datacontext on the UI.
        /// </summary>
        /// <param name="animeList"></param>
        /// <param name="controller"></param>
        public static void Refresh(this AnimeList animeList, XmlController controller)
        {
            var anime = controller.Animes.ToList();
            animeList.DataGrid.ItemsSource = controller.FilteredSortedAnimes();
            animeList.StatsLabel.Content = $"{anime.Count} total animes. " +
                                           $"{anime.Count(a => a.Airing)} airing or watching, " +
                                           $"{anime.Count(a => a.Status.Equals("Finished"))} finished, " +
                                           $"{anime.Count(a => a.Status.Equals("Dropped"))} dropped.";
        }

        /// <summary>
        ///     Check if the container is empty.
        /// </summary>
        /// <param name="textbox"></param>
        /// <returns></returns>
        public static bool Empty(this TextBox textbox) => textbox.Text.Equals("");

        /// <summary>
        ///     Scroll to the bottom.
        /// </summary>
        /// <param name="textbox"></param>
        public static void ScrollDown(this TextBox textbox)
        {
            textbox.Focus();
            textbox.CaretIndex = textbox.Text.Length;
            textbox.ScrollToEnd();
        }

        public static void WriteLine(this TextBox textbox, string text)
        {
            textbox.AppendText(text + "\n");
            textbox.ScrollDown();
        }

        /// <summary>
        ///     Simulate a button press.
        /// </summary>
        /// <param name="button"></param>
        public static void Press(this IInputElement button)
            => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        /// <summary>
        ///     Toggle opacity and visibility of a ButtonSubmit between two states.
        /// </summary>
        /// <param name="button"></param>
        public static void Toggle(this Button button)
        {
            button.Opacity = button.IsHitTestVisible ? 0.4 : 1.0;
            button.IsHitTestVisible ^= true;
        }

        /// <summary>
        ///     Toggle opacity and visibility of all buttons inside the MainWindow.
        /// </summary>
        /// <param name="mainWindow"></param>
        public static void ToggleButtons(this MainWindow mainWindow)
        {
            var buttons = GetDependencyObject<Button>(mainWindow);
            foreach (var button in buttons)
                button.Toggle();
        }

        public static List<T> GetAll<T>(this Grid grid) where T : DependencyObject
        {
            return GetDependencyObject<T>(grid);
        }

        public static List<T> GetAll<T>(this Window window) where T : DependencyObject
        {
            return GetDependencyObject<T>(window);
        }

        public static List<T> GetAll<T>(this UserControl userControl) where T : DependencyObject
        {
            return GetDependencyObject<T>(userControl);
        }

        private static List<T> GetDependencyObject<T>(object parent) where T : DependencyObject
        {
            var logicalCollection = new List<T>();
            GetDependencyObject(parent as DependencyObject, logicalCollection);
            return logicalCollection;
        }

        private static void GetDependencyObject<T>(DependencyObject parent, ICollection<T> logicalCollection)
            where T : DependencyObject
        {
            var children = LogicalTreeHelper.GetChildren(parent);
            foreach (var child in children)
            {
                if (!(child is DependencyObject))
                    continue;
                var dependencyObject = child as DependencyObject;
                if (child is T)
                    logicalCollection.Add(child as T);
                GetDependencyObject(dependencyObject, logicalCollection);
            }
        }
    }
}