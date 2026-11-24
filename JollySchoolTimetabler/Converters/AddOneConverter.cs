using System.Globalization;
using System.Windows.Data;

namespace JollySchoolTimetabler.Converters
{
    public class AddOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue + 1;
            if (value is double doubleValue)
                return doubleValue + 1;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue - 1;
            if (value is double doubleValue)
                return doubleValue - 1;

            return value;
        }
    }
}
