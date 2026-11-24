using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JollySchoolTimetabler.Utils
{
    public static class GlobalListBoxScrollHelper
    {
        private static bool _isHooked;

        public static void EnableGlobalListBoxScrolling()
        {
            if (_isHooked) return;
            _isHooked = true;

            // Attach to all ListBoxes globally
            EventManager.RegisterClassHandler(typeof(ListBox), UIElement.PreviewMouseWheelEvent, new RoutedEventHandler(OnPreviewMouseWheel));
        }

        private static void OnPreviewMouseWheel(object sender, RoutedEventArgs e)
        {
            if (sender is not ListBox listBox || e is not MouseWheelEventArgs mouseArgs)
                return;

            // Find the ScrollViewer within the ListBox
            if (VisualTreeHelper.GetChild(listBox, 0) is Border border && VisualTreeHelper.GetChild(border, 0) is ScrollViewer scrollViewer)
            {
                double offset = scrollViewer.VerticalOffset - mouseArgs.Delta / 3;
                scrollViewer.ScrollToVerticalOffset(offset);
                mouseArgs.Handled = true;
            }
        }
    }
}
