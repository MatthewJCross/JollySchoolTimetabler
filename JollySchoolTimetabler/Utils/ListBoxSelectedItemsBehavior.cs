using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JollySchoolTimetabler.Utils
{
    public static class ListBoxSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(ListBoxSelectedItemsBehavior), new PropertyMetadata(null, OnSelectedItemsChanged));
        public static readonly DependencyProperty MaxSelectedItemsProperty = DependencyProperty.RegisterAttached("MaxSelectedItems", typeof(int), typeof(ListBoxSelectedItemsBehavior), new PropertyMetadata(int.MaxValue));
        public static void SetBindableSelectedItems(DependencyObject element, IList value) => element.SetValue(SelectedItemsProperty, value);
        public static IList GetBindableSelectedItems(DependencyObject element) => (IList)element.GetValue(SelectedItemsProperty);
        public static void SetMaxSelectedItems(DependencyObject element, int value) => element.SetValue(MaxSelectedItemsProperty, value);
        public static int GetMaxSelectedItems(DependencyObject element) => (int)element.GetValue(MaxSelectedItemsProperty);
        public static IList GetSelectedItems(DependencyObject obj) => (IList)obj.GetValue(SelectedItemsProperty);
        public static void SetSelectedItems(DependencyObject obj, IList value) => obj.SetValue(SelectedItemsProperty, value);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListBox listBox)
                return;

            if (e.OldValue is INotifyCollectionChanged oldCollection)
                oldCollection.CollectionChanged -= (_, _) => QueueSync(listBox);

            if (e.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += (_, _) => QueueSync(listBox);

            listBox.SelectionChanged -= ListBox_SelectionChanged;
            listBox.SelectionChanged += ListBox_SelectionChanged;

            QueueSync(listBox);
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox)
                return;

            var boundItems = GetSelectedItems(listBox);
            if (boundItems == null) 
                return;

            int max = GetMaxSelectedItems(listBox);

            // Make safe copies of changes
            var removed = e.RemovedItems.Cast<object>().ToList();
            var added = e.AddedItems.Cast<object>().ToList();

            foreach (var item in added)
            {
                if (boundItems.Count >= max)
                {
                    listBox.SelectedItems.Remove(item);
                    ShowTooltip(listBox, $"You can only select up to {max} subjects.");
                    QueueSync(listBox);
                    break;
                }

                if (!boundItems.Contains(item))
                    boundItems.Add(item);
            }

            foreach (var item in removed)
                boundItems.Remove(item);

        }

        private static void QueueSync(ListBox listBox)
        {
            // Defer to dispatcher AFTER WPF finishes its own internal update
            listBox.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => SyncListBox(listBox)));
        }

        private static void SyncListBox(ListBox listBox)
        {
            var boundItems = GetSelectedItems(listBox);
            if (boundItems == null) 
                return;

            // Snapshot all items to avoid modification during enumeration
            var currentSelection = listBox.SelectedItems.Cast<object>().ToList();
            var desiredSelection = boundItems.Cast<object>().ToList();

            // Only update if something actually changed
            bool same = currentSelection.Count == desiredSelection.Count && !currentSelection.Except(desiredSelection).Any();

            if (same) 
                return;

            // Clear and re-add using deferred dispatcher to avoid reentrancy
            listBox.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                listBox.SelectedItems.Clear();
                foreach (var item in desiredSelection)
                    if (listBox.Items.Contains(item))
                        listBox.SelectedItems.Add(item);
            }));
        }

        private static void ShowTooltip(ListBox listBox, string message)
        {
            var tooltip = new ToolTip
            {
                Content = message,
                PlacementTarget = listBox,
                IsOpen = true,
                StaysOpen = false
            };

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, e) =>
            {
                tooltip.IsOpen = false;
                timer.Stop();
            };

            timer.Start();
        }
    }
}
