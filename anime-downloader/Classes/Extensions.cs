using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using anime_downloader.Views;

namespace anime_downloader.Classes {
    public static class Extensions {

        /// <summary>
        ///     Sort the animes with the specified property name of the anime type
        /// </summary>
        /// <param name="animes"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        /// <remarks>The sort has to be a property of Anime, or else this will fail. This was the
        /// only way I could dynamically change the property of the sort</remarks>
        public static IEnumerable<Anime> SortedWith(this IEnumerable<Anime> animes, string sort) {
            var prop = TypeDescriptor.GetProperties(typeof(Anime)).Find(sort, true);
            return animes.OrderBy(x => prop.GetValue(x));
        }

        public static Anime Find(this IEnumerable<Anime> animes, string name) {
            return (from anime in animes
                    where anime.Name.ToLower().Equals(name.ToLower())
                    select anime).FirstOrDefault();
        }

        /// <summary>
        ///     "Refresh" the datacontext on the UI.
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="data"></param>
        public static void Refresh(this DataGrid dataGrid, IEnumerable<Anime> data) {
            dataGrid.ItemsSource = data;
        }
        
        /// <summary>
        ///     Check if the container is empty.
        /// </summary>
        /// <param name="textbox"></param>
        /// <returns></returns>
        public static bool Empty(this TextBox textbox) {
            return textbox.Text.Equals("");
        }

        /// <summary>
        ///     Scroll to the bottom.
        /// </summary>
        /// <param name="textbox"></param>
        public static void ScrollDown(this TextBox textbox) {
            textbox.Focus();
            textbox.CaretIndex = textbox.Text.Length;
            textbox.ScrollToEnd();
        }

        /// <summary>
        ///     Simulate a button press.
        /// </summary>
        /// <param name="button"></param>
        public static void Press(this IInputElement button) {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        /// <summary>
        ///     Toggle opacity and visibility of a button between two states.
        /// </summary>
        /// <param name="button"></param>
        public static void Toggle(this Button button) {
            if (button.IsHitTestVisible) {
                button.IsHitTestVisible = false;
                button.Opacity = 0.4;
            }
            else {
                button.IsHitTestVisible = true;
                button.Opacity = 1.0;
            }
        }

        /// <summary>
        ///     Toggle opacity and visibility of all buttons inside the MainWindow.
        /// </summary>
        /// <param name="mainWindow"></param>
        public static void ToggleButtons(this MainWindow mainWindow) {
            var buttons = GetLogicalChildCollection<Button>(mainWindow);
            foreach (var button in buttons)
                button.Toggle();
        }

        /// <summary>
        ///     Toggle opacity and visibility of arbitrary amount of buttons.
        /// </summary>
        /// <param name="mainWindow"></param>
        /// <param name="buttons"></param>
        public static void ToggleButtons(this MainWindow mainWindow, params Button[] buttons) {
            foreach (var button in buttons)
                button.Toggle();
        }

        public static List<T> GetAll<T>(this Window window) where T : DependencyObject {
            return GetLogicalChildCollection<T>(window);
        }

        public static List<T> GetAll<T>(this UserControl userControl) where T : DependencyObject {
            return GetLogicalChildCollection<T>(userControl);
        } 

        private static List<T> GetLogicalChildCollection<T>(object parent) where T : DependencyObject {
            var logicalCollection = new List<T>();
            GetLogicalChildCollection(parent as DependencyObject, logicalCollection);
            return logicalCollection;
        }

        private static void GetLogicalChildCollection<T>(DependencyObject parent, ICollection<T> logicalCollection) where T : DependencyObject {
            var children = LogicalTreeHelper.GetChildren(parent);

            foreach (var child in children) {

                if (!(child is DependencyObject))
                    continue;

                var dependencyObject = child as DependencyObject;

                if (child is T) {
                    logicalCollection.Add(child as T);
                }

                GetLogicalChildCollection(dependencyObject, logicalCollection);
            }
        }
    }
}
