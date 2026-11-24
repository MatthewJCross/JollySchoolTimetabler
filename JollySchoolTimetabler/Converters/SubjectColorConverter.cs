using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JollySchoolTimetabler.Converters
{
    public class SubjectColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Brushes.LightGray;

            int key = value is int id ? id : value.GetHashCode();

            // Generate a pastel colour from hash
            byte r = (byte)((key * 37) % 180 + 50);
            byte g = (byte)((key * 67) % 180 + 50);
            byte b = (byte)((key * 97) % 180 + 50);

            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
