using JollySchoolTimetabler.Enums;
using JollySchoolTimetabler.Models;
using JollySchoolTimetabler.ViewModels;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace JollySchoolTimetabler.Converters
{
    public class TimetableEntryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length < 7)
                    return null;

                if (values[0] is not DayOfWeek day || values[1] is not int period)
                    return string.Empty;

                day = (DayOfWeek)values[0];
                period = (int)values[1];


                var entries = values[2] as IEnumerable<TimetableEntry>;
                if (entries == null)
                    return null;

                if (values[3] is not TimetableMode mode)
                    return null;

                var subjects = values[4] as IEnumerable<Subject>;
                var teachers = values[5] as IEnumerable<Teacher>;
                var rooms = values[6] as IEnumerable<Room>;

                Debug.WriteLine($"[Converter] Day={day}, Period={period}, Mode={mode}");

                var entry = entries?.FirstOrDefault(e => e.DayOfWeek == day && e.Period == period);
                if (entry == null)
                    return string.Empty;

                string subject = subjects?.FirstOrDefault(s => s.SubjectId == entry.SubjectId)?.Name ?? "Unknown";
                string teacher = teachers?.FirstOrDefault(t => t.TeacherId == entry.TeacherId)?.FullName ?? "";
                string room = rooms?.FirstOrDefault(r => r.RoomId == entry.RoomId)?.Name ?? "";

                return mode switch
                {
                    TimetableMode.Student => $"{subject}\n{teacher}\n{room}",
                    TimetableMode.Teacher => $"{subject}\n{room}",
                    TimetableMode.Class => $"{subject}\n{teacher}\n{room}",
                    TimetableMode.Room => $"{subject}\n{teacher}",
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TimetableEntryConverter] Error: {ex.Message}");
                return string.Empty;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
