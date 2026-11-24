namespace JollySchoolTimetabler.Services.Scheduling
{
    /// <summary>Necessary capacity checks and search-risk indicators. Risks are not explanations of CP-SAT conflicts.</summary>
    public static class TimetableConstraintDiagnostics
    {
        public sealed record Report(IReadOnlyList<string> Lines, IReadOnlyList<string> Errors, IReadOnlyList<string> Risks);

        public static Report Analyse(IReadOnlyList<TimetableSolver.Group> groups, int periodsPerDay)
        {
            int slots = 5 * periodsPerDay;
            var errors = new List<string>();
            var risks = new List<(double Score, string Message)>();
            var lines = new List<string> { "Constraint diagnostics (necessary capacity checks; risks are not proven causes):" };
            int total = groups.Sum(g => g.Periods);
            var teachers = groups.SelectMany(g => g.TeacherIds).Distinct().ToArray();
            var rooms = groups.SelectMany(g => g.RoomIds).Distinct().ToArray();
            lines.Add($"Demand: {total} lesson-periods; {teachers.Length} eligible teachers; {rooms.Length} eligible rooms; {slots} weekly slots.");
            if (total > teachers.Length * slots)
                errors.Add($"Teacher capacity impossible: {total} periods require more than {teachers.Length * slots} teacher-slots.");

            if (total > rooms.Length * slots)
                errors.Add($"Room capacity impossible: {total} periods require more than {rooms.Length * slots} room-slots.");

            // A group restricted to a subset of teachers/rooms consumes capacity from that subset.
            // The same necessary bound applies to all groups whose options are contained in a given set.
            foreach (var bucket in groups.GroupBy(g => string.Join(",", g.TeacherIds.OrderBy(x => x))))
            {
                var options = bucket.First().TeacherIds.Distinct().ToHashSet();
                int demand = groups.Where(g => g.TeacherIds.All(options.Contains)).Sum(g => g.Periods);
                int capacity = options.Count * slots;
                string names = string.Join(", ", bucket.Take(3).Select(g => g.TeachingClass.Name));
                if (demand > capacity)
                    errors.Add($"Teacher pool [{string.Join(",", options.OrderBy(x => x))}] overloaded: {demand}/{capacity} periods; e.g. {names}.");
                else if (capacity > 0 && demand >= capacity * 0.75)
                    risks.Add(((double)demand / capacity, $"Teacher pool [{string.Join(",", options.OrderBy(x => x))}]: {demand}/{capacity} slots ({(double)demand / capacity:P0} utilisation); e.g. {names}."));
            }

            foreach (var bucket in groups.GroupBy(g => string.Join(",", g.RoomIds.OrderBy(x => x))))
            {
                var options = bucket.First().RoomIds.Distinct().ToHashSet();
                int demand = groups.Where(g => g.RoomIds.All(options.Contains)).Sum(g => g.Periods);
                int capacity = options.Count * slots;
                string names = string.Join(", ", bucket.Take(3).Select(g => g.TeachingClass.Name));
                if (demand > capacity)
                    errors.Add($"Room pool [{string.Join(",", options.OrderBy(x => x))}] overloaded: {demand}/{capacity} periods; e.g. {names}.");
                else if (capacity > 0 && demand >= capacity * 0.75)
                    risks.Add(((double)demand / capacity, $"Room pool [{string.Join(",", options.OrderBy(x => x))}]: {demand}/{capacity} slots ({(double)demand / capacity:P0} utilisation); e.g. {names}."));
            }

            foreach (var student in groups.SelectMany(g => g.StudentIds).Distinct())
            {
                int demand = groups.Where(g => Array.IndexOf(g.StudentIds, student) >= 0).Sum(g => g.Periods);
                if (demand > slots)
                    errors.Add($"Student {student} overloaded: {demand}/{slots} weekly slots.");
                else if (demand >= slots * 0.9)
                    risks.Add(((double)demand / slots, $"Student {student}: {demand}/{slots} slots occupied; little scheduling flexibility."));
            }

            foreach (var g in groups)
            {
                if (g.TeacherIds.Distinct().Count() == 1 || g.RoomIds.Distinct().Count() == 1)
                    risks.Add((1.0 / Math.Max(1, g.TeacherIds.Length * g.RoomIds.Length), $"{g.TeachingClass.Name}: {g.Periods} periods, {g.TeacherIds.Distinct().Count()} teacher option(s), {g.RoomIds.Distinct().Count()} room option(s)."));
            }

            lines.AddRange(errors.Select(e => "PROVEN CAPACITY FAILURE: " + e));
            var top = risks.OrderByDescending(x => x.Score).Take(15).Select(x => x.Message).ToArray();
            lines.Add($"Potential bottlenecks ({risks.Count} indicators; showing up to 15):");
            lines.AddRange(top.Select(r => "RISK: " + r));
            if (risks.Count == 0)
                lines.Add("No obvious high-utilisation or single-option bottlenecks found.");

            lines.Add("Note: CP-SAT cannot attribute its conflicts to individual teachers, rooms or classes. These are independent capacity checks and heuristics.");
            return new(lines, errors, top);
        }
    }
}