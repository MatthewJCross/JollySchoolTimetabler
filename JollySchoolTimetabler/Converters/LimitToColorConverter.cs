using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JollySchoolTimetabler.Converters
{
    public class LimitToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Brushes.Transparent;

            if (!int.TryParse(parameter?.ToString(), out int limit))
                limit = 9; // default

            int count = 0;
            if (value is int intVal)
                count = intVal;
            else if (int.TryParse(value.ToString(), out int parsed))
                count = parsed;

            return count >= limit ? Brushes.Red : Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
