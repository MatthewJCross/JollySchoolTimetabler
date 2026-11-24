using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using JollySchoolTimetabler.Enums;

namespace JollySchoolTimetabler.Converters
{
    public class TimetableModeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TimetableMode mode)
                return Brushes.White;

            return mode switch
            {
                TimetableMode.Student => new SolidColorBrush(Color.FromRgb(214, 233, 255)),  // Light blue
                TimetableMode.Teacher => new SolidColorBrush(Color.FromRgb(214, 255, 216)),  // Light green
                TimetableMode.Class => new SolidColorBrush(Color.FromRgb(255, 250, 214)),    // Light yellow
                TimetableMode.Room => new SolidColorBrush(Color.FromRgb(255, 214, 214)),     // Light red
                _ => Brushes.White
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
