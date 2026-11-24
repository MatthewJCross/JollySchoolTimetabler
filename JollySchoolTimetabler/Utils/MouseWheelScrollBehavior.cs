using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JollySchoolTimetabler.Utils
{
    public static class MouseWheelScrollBehavior
    {
        public static bool GetEnableMouseWheel(DependencyObject obj) => (bool)obj.GetValue(EnableMouseWheelProperty);
        public static void SetEnableMouseWheel(DependencyObject obj, bool value) => obj.SetValue(EnableMouseWheelProperty, value);

        public static readonly DependencyProperty EnableMouseWheelProperty = DependencyProperty.RegisterAttached("EnableMouseWheel", typeof(bool), typeof(MouseWheelScrollBehavior), new PropertyMetadata(false, OnEnableMouseWheelChanged));

        private static void OnEnableMouseWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                    listBox.PreviewMouseWheel += ListBox_PreviewMouseWheel;
                else
                    listBox.PreviewMouseWheel -= ListBox_PreviewMouseWheel;
            }
        }

        private static void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                if (VisualTreeHelper.GetChild(listBox, 0) is Border border && VisualTreeHelper.GetChild(border, 0) is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
                    e.Handled = true;
                }
            }
        }
    }
}
