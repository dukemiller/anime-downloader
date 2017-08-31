using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using GalaSoft.MvvmLight;

namespace anime_downloader.Classes.Xaml
{
    public class MultiSelectionBehavior : Behavior<DataGrid>
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(IList), typeof(MultiSelectionBehavior),
                new UIPropertyMetadata(null, SelectedItemsChanged));

        private bool _isUpdatingSource;

        private bool _isUpdatingTarget;

        public IList SelectedItems
        {
            get => (IList) GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (SelectedItems != null)
            {
                AssociatedObject.SelectedItems.Clear();
                foreach (var item in SelectedItems)
                    AssociatedObject.SelectedItems.Add(item);
            }
        }

        private static void SelectedItemsChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModelBase.IsInDesignModeStatic)
                return;

            var behavior = o as MultiSelectionBehavior;
            if (behavior == null)
                return;

            var oldValue = e.OldValue as INotifyCollectionChanged;
            var newValue = e.NewValue as INotifyCollectionChanged;

            if (oldValue != null)
            {
                oldValue.CollectionChanged -= behavior.SourceCollectionChanged;
                behavior.AssociatedObject.SelectionChanged -= behavior.ListBoxSelectionChanged;
            }
            if (newValue != null)
            {
                behavior.AssociatedObject.SelectedItems.Clear();
                foreach (var item in (IEnumerable) newValue)
                    behavior.AssociatedObject.SelectedItems.Add(item);

                behavior.AssociatedObject.SelectionChanged += behavior.ListBoxSelectionChanged;
                newValue.CollectionChanged += behavior.SourceCollectionChanged;
            }
        }

        private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isUpdatingSource)
                return;

            try
            {
                _isUpdatingTarget = true;

                if (e.OldItems != null)
                    foreach (var item in e.OldItems)
                        AssociatedObject.SelectedItems.Remove(item);

                if (e.NewItems != null)
                    foreach (var item in e.NewItems)
                        AssociatedObject.SelectedItems.Add(item);

                if (e.Action == NotifyCollectionChangedAction.Reset)
                    AssociatedObject.SelectedItems.Clear();
            }
            finally
            {
                _isUpdatingTarget = false;
            }
        }

        private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingTarget)
                return;

            var selectedItems = SelectedItems;
            if (selectedItems == null)
                return;

            try
            {
                _isUpdatingSource = true;

                foreach (var item in e.RemovedItems)
                    selectedItems.Remove(item);

                foreach (var item in e.AddedItems)
                    selectedItems.Add(item);
            }
            finally
            {
                _isUpdatingSource = false;
            }
        }
    }
}