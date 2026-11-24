namespace JollySchoolTimetabler.Services.Scheduling
{
    public sealed class TimetableValidator
    {
        public List<string> Validate(IReadOnlyList<TimetableSolver.Group> groups, IReadOnlyList<TimetableSolver.Placement> placements, int periodsPerDay)
        {
            var errors = new List<string>();
            if (placements.Count != groups.Sum(g => g.Periods)) 
                errors.Add("Total lesson count is incorrect.");

            foreach (var group in groups)
            {
                var assigned = placements.Where(p => p.GroupIndex == group.Index).ToList();
                if (assigned.Count != group.Periods) 
                    errors.Add($"Incorrect lesson count: {group.TeachingClass.Name}");

                foreach (var p in assigned)
                {
                    if (!group.TeacherIds.Contains(p.TeacherId) || !group.RoomIds.Contains(p.RoomId))
                        errors.Add($"Ineligible teacher or room: {group.TeachingClass.Name}");

                    if (p.Day < DayOfWeek.Monday || p.Day > DayOfWeek.Friday || p.Period < 1 || p.Period > periodsPerDay)
                        errors.Add($"Invalid period: {group.TeachingClass.Name}");
                }

                if (group.DoublePeriod)
                {
                    var remaining = assigned.ToDictionary(p => (p.Day, p.Period));
                    foreach (var p in assigned.OrderBy(p => p.Day).ThenBy(p => p.Period))
                    {
                        if (!remaining.Remove((p.Day, p.Period))) 
                            continue;

                        if (!remaining.TryGetValue((p.Day, p.Period + 1), out var next) || next.TeacherId != p.TeacherId || next.RoomId != p.RoomId)
                            errors.Add($"Broken double period: {group.TeachingClass.Name}");
                        else 
                            remaining.Remove((next.Day, next.Period));
                    }
                }
            }

            foreach (var slot in placements.GroupBy(p => (p.Day, p.Period)))
            {
                if (slot.GroupBy(p => p.TeacherId).Any(g => g.Count() > 1)) 
                    errors.Add($"Teacher clash: {slot.Key}");

                if (slot.GroupBy(p => p.RoomId).Any(g => g.Count() > 1)) 
                    errors.Add($"Room clash: {slot.Key}");

                if (slot.SelectMany(p => groups[p.GroupIndex].StudentIds).GroupBy(id => id).Any(g => g.Count() > 1))
                    errors.Add($"Student clash: {slot.Key}");
            }

            return errors;
        }
    }
}