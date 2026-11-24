using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using JollySchoolTimetabler.Enums;

namespace JollySchoolTimetabler.Converters
{
    public class TimetableModeToTextBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TimetableMode mode)
                return Brushes.Black;

            // Darker text for light backgrounds
            return mode switch
            {
                TimetableMode.Student => Brushes.DarkBlue,
                TimetableMode.Teacher => Brushes.DarkGreen,
                TimetableMode.Class => Brushes.DarkGoldenrod,
                TimetableMode.Room => Brushes.DarkRed,
                _ => Brushes.Black
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
