using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace anime_downloader.Classes
{
    public static class Extensions
    {

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

        public static void Toggle(this IEnumerable<Control> controls)
        {
            foreach(var control in controls)
                control.Toggle();
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

        public static T GetParentUntil<T>(this DependencyObject child) where T : DependencyObject
        {
            var parent = child;

            while (parent != null)
            {
                if (parent is ListBox)
                    break;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return (T) parent;
        }
        
        public static string OnlyLettersAndSpace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsLetter(c) || !char.IsWhiteSpace(c))
                .ToArray());
        }

        public static bool IsBlank(this string str)
        {
            return str == null || str.Equals("");
        }
    }
}