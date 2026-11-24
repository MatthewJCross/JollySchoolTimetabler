using System.Globalization;
using System.Windows;
using System.Windows.Data;
using JollySchoolTimetabler.Enums;

namespace JollySchoolTimetabler.Converters
{
    public class ModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimetableMode mode && parameter is string param)
            {
                if (mode == TimetableMode.Student && param == "Student")
                    return Visibility.Visible;
                if (mode == TimetableMode.Teacher && param == "Teacher")
                    return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
