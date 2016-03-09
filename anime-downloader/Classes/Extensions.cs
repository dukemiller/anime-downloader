using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace anime_downloader.Classes {
    public static class Extensions {

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
