using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace anime_downloader.Classes.Xaml
{
    // http://stackoverflow.com/questions/34748875/binding-a-collection-to-selecteditems-in-a-listbox-without-violating-mvvm
    public static class AttachedProperties
    {
        #region AttachedProperties.SelectedItems Attached Property

        public static IList GetSelectedItems(ListBox obj)
        {
            return (IList) obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(ListBox obj, IList value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        public static readonly DependencyProperty
            SelectedItemsProperty =
                DependencyProperty.RegisterAttached(
                    "SelectedItems",
                    typeof(IList),
                    typeof(AttachedProperties),
                    new PropertyMetadata(null,
                        SelectedItems_PropertyChanged));

        private static void SelectedItems_PropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var lb = d as ListBox;
            var coll = e.NewValue as IList;

            //  If you want to go both ways and have changes to 
            //  this collection reflected back into the listbox...
            if (coll is INotifyCollectionChanged)
                (coll as INotifyCollectionChanged)
                    .CollectionChanged += (s, e3) =>
                    {
                        //  Haven't tested this branch -- good luck!
                        if (null != e3.OldItems)
                            foreach (var item in e3.OldItems)
                                lb.SelectedItems.Remove(item);
                        if (null != e3.NewItems)
                            foreach (var item in e3.NewItems)
                                lb.SelectedItems.Add(item);
                    };

            if (null != coll)
            {
                if (coll.Count > 0)
                {
                    //  Minor problem here: This doesn't work for initializing a 
                    //  selection on control creation. 
                    //  When I get here, it's because I've initialized the selected 
                    //  items collection that I'm binding. But at that point, lb.Items 
                    //  isn't populated yet, so adding these items to lb.SelectedItems 
                    //  always fails. 
                    //  Haven't tested this otherwise -- good luck!
                    lb.SelectedItems.Clear();
                    foreach (var item in coll)
                        lb.SelectedItems.Add(item);
                }

                lb.SelectionChanged += (s, e2) =>
                {
                    if (null != e2.RemovedItems)
                        foreach (var item in e2.RemovedItems)
                            coll.Remove(item);
                    if (null != e2.AddedItems)
                        foreach (var item in e2.AddedItems)
                            coll.Add(item);
                };
            }
        }

        #endregion AttachedProperties.SelectedItems Attached Property
    }

    // http://stackoverflow.com/questions/9880589/bind-to-selecteditems-from-datagrid-or-listbox-in-mvvm
    public class Ex : DependencyObject
    {
        public static readonly DependencyProperty IsSubscribedToSelectionChangedProperty = DependencyProperty
            .RegisterAttached(
                "IsSubscribedToSelectionChanged", typeof(bool), typeof(Ex), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
            "SelectedItems", typeof(IList), typeof(Ex), new PropertyMetadata(default(IList), OnSelectedItemsChanged));

        public static void SetIsSubscribedToSelectionChanged(DependencyObject element, bool value)
        {
            element.SetValue(IsSubscribedToSelectionChangedProperty, value);
        }

        public static bool GetIsSubscribedToSelectionChanged(DependencyObject element)
        {
            return (bool) element.GetValue(IsSubscribedToSelectionChangedProperty);
        }

        public static void SetSelectedItems(DependencyObject element, IList value)
        {
            element.SetValue(SelectedItemsProperty, value);
        }

        public static IList GetSelectedItems(DependencyObject element)
        {
            return (IList) element.GetValue(SelectedItemsProperty);
        }

        /// <summary>
        ///     Attaches a list or observable collection to the grid or listbox, syncing both lists (one way sync for simple
        ///     lists).
        /// </summary>
        /// <param name="d">The DataGrid or ListBox</param>
        /// <param name="e">The list to sync to.</param>
        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ListBox || d is MultiSelector))
                throw new ArgumentException(
                    "Somehow this got attached to an object I don't support. ListBoxes and Multiselectors (DataGrid), people. Geesh =P!");

            var selector = (Selector) d;
            var oldList = e.OldValue as IList;
            if (oldList != null)
            {
                var obs = oldList as INotifyCollectionChanged;
                if (obs != null)
                    obs.CollectionChanged -= OnCollectionChanged;
                // If we're orphaned, disconnect lb/dg events.
                if (e.NewValue == null)
                {
                    selector.SelectionChanged -= OnSelectorSelectionChanged;
                    SetIsSubscribedToSelectionChanged(selector, false);
                }
            }
            var newList = (IList) e.NewValue;
            if (newList != null)
            {
                var obs = newList as INotifyCollectionChanged;
                if (obs != null)
                    obs.CollectionChanged += OnCollectionChanged;
                PushCollectionDataToSelectedItems(newList, selector);
                var isSubscribed = GetIsSubscribedToSelectionChanged(selector);
                if (!isSubscribed)
                {
                    selector.SelectionChanged += OnSelectorSelectionChanged;
                    SetIsSubscribedToSelectionChanged(selector, true);
                }
            }
        }

        /// <summary>
        ///     Initially set the selected items to the items in the newly connected collection,
        ///     unless the new collection has no selected items and the listbox/grid does, in which case
        ///     the flow is reversed. The data holder sets the state. If both sides hold data, then the
        ///     bound IList wins and dominates the helpless wpf control.
        /// </summary>
        /// <param name="obs">The list to sync to</param>
        /// <param name="selector">The grid or listbox</param>
        private static void PushCollectionDataToSelectedItems(IList obs, DependencyObject selector)
        {
            var listBox = selector as ListBox;
            if (listBox != null)
            {
                if (obs.Count > 0)
                {
                    listBox.SelectedItems.Clear();
                    foreach (var ob in obs) listBox.SelectedItems.Add(ob);
                }
                else
                {
                    foreach (var ob in listBox.SelectedItems) obs.Add(ob);
                }
                return;
            }
            // Maybe other things will use the multiselector base... who knows =P
            var grid = selector as MultiSelector;
            if (grid != null)
            {
                if (obs.Count > 0)
                {
                    grid.SelectedItems.Clear();
                    foreach (var ob in obs) grid.SelectedItems.Add(ob);
                }
                else
                {
                    foreach (var ob in grid.SelectedItems) obs.Add(ob);
                }
                return;
            }
            throw new ArgumentException(
                "Somehow this got attached to an object I don't support. ListBoxes and Multiselectors (DataGrid), people. Geesh =P!");
        }

        /// <summary>
        ///     When the listbox or grid fires a selectionChanged even, we update the attached list to
        ///     match it.
        /// </summary>
        /// <param name="sender">The listbox or grid</param>
        /// <param name="e">Items added and removed.</param>
        private static void OnSelectorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dep = (DependencyObject) sender;
            var items = GetSelectedItems(dep);
            var col = items as INotifyCollectionChanged;

            // Remove the events so we don't fire back and forth, then re-add them.
            if (col != null) col.CollectionChanged -= OnCollectionChanged;
            foreach (var oldItem in e.RemovedItems) items.Remove(oldItem);
            foreach (var newItem in e.AddedItems) items.Add(newItem);
            if (col != null) col.CollectionChanged += OnCollectionChanged;
        }

        /// <summary>
        ///     When the attached object implements INotifyCollectionChanged, the attached listbox
        ///     or grid will have its selectedItems adjusted by this handler.
        /// </summary>
        /// <param name="sender">The listbox or grid</param>
        /// <param name="e">The added and removed items</param>
        private static void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Push the changes to the selected item.
            var listbox = sender as ListBox;
            if (listbox != null)
            {
                listbox.SelectionChanged -= OnSelectorSelectionChanged;
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    listbox.SelectedItems.Clear();
                }
                else
                {
                    foreach (var oldItem in e.OldItems) listbox.SelectedItems.Remove(oldItem);
                    foreach (var newItem in e.NewItems) listbox.SelectedItems.Add(newItem);
                }
                listbox.SelectionChanged += OnSelectorSelectionChanged;
            }
            var grid = sender as MultiSelector;
            if (grid != null)
            {
                grid.SelectionChanged -= OnSelectorSelectionChanged;
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    grid.SelectedItems.Clear();
                }
                else
                {
                    foreach (var oldItem in e.OldItems) grid.SelectedItems.Remove(oldItem);
                    foreach (var newItem in e.NewItems) grid.SelectedItems.Add(newItem);
                }
                grid.SelectionChanged += OnSelectorSelectionChanged;
            }
        }
    }
}