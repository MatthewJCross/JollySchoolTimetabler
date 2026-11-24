using System.IO;
using System.Text.Json;

namespace JollySchoolTimetabler.Services.Scheduling
{
    public sealed class CurriculumRule
    {
        public int YearGroupId { get; set; }
        public int SubjectId { get; set; }
        public int WeeklyPeriods { get; set; } = 1;
        public bool DoublePeriods { get; set; }
        public bool Compulsory { get; set; }
        public int PreferredClassSize { get; set; } = 30;
        public int MaximumClassSize { get; set; } = 35;
    }

    public static class CurriculumSettings
    {
        private static readonly string PathName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JollySchoolTimetabler", "curriculum.json");

        public static List<CurriculumRule> Load()
        {
            if (!File.Exists(PathName))
                return new();

            return JsonSerializer.Deserialize<List<CurriculumRule>>(File.ReadAllText(PathName)) ?? new();
        }

        public static void Save(IEnumerable<CurriculumRule> rules)
        {
            var values = rules.ToList();
            foreach (var rule in values)
            {
                if (rule.WeeklyPeriods < 0 || rule.WeeklyPeriods > 30)
                    throw new InvalidOperationException("Weekly periods must be between 0 and 30.");

                if (rule.PreferredClassSize < 1 || rule.MaximumClassSize < rule.PreferredClassSize || rule.MaximumClassSize > 35)
                    throw new InvalidOperationException("Class sizes must be positive, preferred <= maximum, and maximum <= 35.");

                if (rule.DoublePeriods && rule.WeeklyPeriods % 2 != 0)
                    throw new InvalidOperationException("Double-period subjects require an even number of weekly periods.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
            var temp = PathName + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, PathName, true);
        }

        public static readonly string[] CoreCodes = { "ENG", "MATH", "SCI", "HIS", "GEO", "PE", "COMP" };
        public static readonly string[] OptionCodes = { "ART", "MUS", "DRM", "DT", "FRN", "SPN", "BUS", "RE" };

        public static int DefaultPeriods(string? code) => code switch
        {
            "ENG" or "MATH" or "SCI" => 5,
            "HIS" or "GEO" or "PE" => 3,
            "COMP" => 2,
            _ => 1
        };

        // Explicitly reset the separate per-user curriculum file when test data is reseeded.
        // This is intentionally NOT done on ordinary application startup or timetable generation.
        public static void ResetForSeed(IEnumerable<int> yearIds, IEnumerable<(int Id, string? Code)> subjects)
        {
            var rules = (from year in yearIds
                         from subject in subjects
                         select new CurriculumRule
                         {
                             YearGroupId = year,
                             SubjectId = subject.Id,
                             WeeklyPeriods = DefaultPeriods(subject.Code),
                             DoublePeriods = false,
                             Compulsory = CoreCodes.Contains(subject.Code),
                             PreferredClassSize = 30,
                             MaximumClassSize = 35
                         }).ToList();
            Save(rules);
        }

        // The smallest number of groups is preferred, provided this does not create an
        // undersized remainder. Balance all groups, never create a tiny last class.
        public static int GroupCount(int studentCount, int preferred, int maximum)
        {
            if (studentCount <= 0)
                return 0;

            int preferredCount = (studentCount + preferred - 1) / preferred;
            int minimumCount = (studentCount + maximum - 1) / maximum;
            // Only use the overflow allowance if it eliminates a tiny residual class.
            if (minimumCount < preferredCount && studentCount % preferred is > 0 and <= 10)
                return minimumCount;

            return preferredCount;
        }
    }
}