using Google.OrTools.Sat;
using System.Diagnostics;
using JollySchoolTimetabler.Models;

namespace JollySchoolTimetabler.Services.Scheduling
{
    /// <summary>Builds a complete school timetable, or returns no assignments. No database access.</summary>
    public sealed class TimetableSolver
    {
        public sealed record Group(int Index, Class TeachingClass, int[] StudentIds, int Periods, bool DoublePeriod, int[] TeacherIds, int[] RoomIds, int OptionBlock = -1);
        public sealed record Placement(int GroupIndex, DayOfWeek Day, int Period, int TeacherId, int RoomId);
        public sealed record Solution(bool Success, string Status, List<Placement> Placements);

        public Solution Solve(IReadOnlyList<Group> groups, int periodsPerDay, double seconds = 90, IProgress<TimetableProgress>? progress = null, bool allowPartial = false)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(periodsPerDay, 1);

            progress?.Report(new("4/8 · Building solver model", $"Creating lesson, teacher and room variables for {groups.Count} groups"));
            var model = new CpModel();
            const int days = 5;
            int slotCount = days * periodsPerDay;
            var used = new BoolVar[groups.Count, slotCount];
            var teacher = new Dictionary<(int g, int slot, int teacher), BoolVar>();
            var room = new Dictionary<(int g, int slot, int room), BoolVar>();
            var starts = new Dictionary<(int g, int slot), BoolVar>();

            for (int g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                if (group.Periods < 1 || group.Periods > slotCount || group.TeacherIds.Length == 0 || group.RoomIds.Length == 0)
                    return new(false, $"Invalid or unschedulable group: {group.TeachingClass.Name}", []);

                if (group.DoublePeriod && (periodsPerDay < 2 || group.Periods % 2 != 0))
                    return new(false, $"Double-period subject requires an even number of periods: {group.TeachingClass.Name}", []);

                for (int s = 0; s < slotCount; s++)
                {
                    used[g, s] = model.NewBoolVar($"lesson_{g}_{s}");
                    var ts = new List<BoolVar>();
                    foreach (int t in group.TeacherIds.Distinct())
                    {
                        var v = model.NewBoolVar($"teacher_{g}_{s}_{t}");
                        teacher[(g, s, t)] = v;
                        ts.Add(v);
                    }

                    model.Add(LinearExpr.Sum(ts) == used[g, s]);
                    var rs = new List<BoolVar>();
                    foreach (int r in group.RoomIds.Distinct())
                    {
                        var v = model.NewBoolVar($"room_{g}_{s}_{r}");
                        room[(g, s, r)] = v;
                        rs.Add(v);
                    }

                    model.Add(LinearExpr.Sum(rs) == used[g, s]);
                }
                if (allowPartial)
                    model.Add(LinearExpr.Sum(Enumerable.Range(0, slotCount).Select(s => used[g, s])) <= group.Periods);
                else
                    model.Add(LinearExpr.Sum(Enumerable.Range(0, slotCount).Select(s => used[g, s])) == group.Periods);

                if (group.DoublePeriod)
                {
                    var pairStarts = new List<BoolVar>();
                    for (int s = 0; s < slotCount; s++)
                    {
                        if (s % periodsPerDay == periodsPerDay - 1)
                            continue;

                        var v = model.NewBoolVar($"pair_{g}_{s}");
                        starts[(g, s)] = v;
                        pairStarts.Add(v);

                        // One teacher and room must stay with the class for both periods.
                        foreach (int t in group.TeacherIds.Distinct())
                            model.Add(teacher[(g, s, t)] == teacher[(g, s + 1, t)]).OnlyEnforceIf(v);

                        foreach (int r in group.RoomIds.Distinct())
                            model.Add(room[(g, s, r)] == room[(g, s + 1, r)]).OnlyEnforceIf(v);
                    }

                    model.Add(LinearExpr.Sum(pairStarts) <= group.Periods / 2);
                    if (!allowPartial)
                        model.Add(LinearExpr.Sum(pairStarts) == group.Periods / 2);

                    for (int s = 0; s < slotCount; s++)
                    {
                        var covering = new List<BoolVar>();
                        if (starts.TryGetValue((g, s), out var first))
                            covering.Add(first);

                        if (s % periodsPerDay != 0 && starts.TryGetValue((g, s - 1), out var previous))
                            covering.Add(previous);

                        model.Add(used[g, s] == LinearExpr.Sum(covering));
                    }
                }
            }

            // A block is one common period shared by two disjoint option pathways.
            // All groups assigned to the block must meet in exactly the same slot.
            // A year has four distinct blocks, one per selected optional subject.
            var optionBlockSlots = new Dictionary<(int year, int block, int slot), BoolVar>();
            var blockKeys = groups.Where(g => g.OptionBlock >= 0)
                .Select(g => (year: g.TeachingClass.YearGroupId, block: g.OptionBlock))
                .Distinct().ToArray();
            if (!allowPartial) foreach (var (year, block) in blockKeys)
            {
                var choices = new List<BoolVar>();
                for (int slot = 0; slot < slotCount; slot++)
                {
                    var choice = model.NewBoolVar($"option_{year}_{block}_{slot}");
                    optionBlockSlots[(year, block, slot)] = choice;
                    choices.Add(choice);
                    foreach (var g in groups.Where(g => g.TeachingClass.YearGroupId == year && g.OptionBlock == block))
                        model.Add(used[g.Index, slot] == choice);
                }
                model.Add(LinearExpr.Sum(choices) == 1);
            }
            // Diagnostic relaxation must not require option blocks to be present;
            // it searches without block synchronisation to expose pressure.
            if (!allowPartial) foreach (var year in blockKeys.Select(x => x.year).Distinct())
                for (int slot = 0; slot < slotCount; slot++)
                    model.AddAtMostOne(blockKeys.Where(k => k.year == year)
                        .Select(k => optionBlockSlots[(year, k.block, slot)]));

            progress?.Report(new("4/8 · Conflict constraints", $"Adding per-slot teacher, room and student exclusions for {slotCount} weekly slots"));
            for (int s = 0; s < slotCount; s++)
            {
                foreach (var teacherGroup in teacher.Where(x => x.Key.slot == s).GroupBy(x => x.Key.teacher))
                    model.AddAtMostOne(teacherGroup.Select(x => x.Value));

                foreach (var roomGroup in room.Where(x => x.Key.slot == s).GroupBy(x => x.Key.room))
                    model.AddAtMostOne(roomGroup.Select(x => x.Value));

                foreach (var studentGroup in groups.SelectMany((g, i) => g.StudentIds.Select(id => (id, i))).GroupBy(x => x.id))
                    model.AddAtMostOne(studentGroup.Select(x => used[x.i, s]));
            }

            progress?.Report(new("4/8 · Model ready", $"{groups.Count} groups · {groups.Sum(g => g.Periods)} lesson-periods · {teacher.Count} teacher choices · {room.Count} room choices · {starts.Count} double-period starts"));
            if (allowPartial)
                model.Maximize(LinearExpr.Sum(Enumerable.Range(0, groups.Count).SelectMany(g => Enumerable.Range(0, slotCount).Select(slot => used[g, slot]))));

            var solver = new CpSolver { StringParameters = $"max_time_in_seconds:{seconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} num_search_workers:8 log_search_progress:true" };
            var watch = Stopwatch.StartNew();

            // Google.OrTools 9.14 C# does not expose CpSolver.LogCallback.
            // Keep search progress honest: the solver reports statistics after Solve returns.
            progress?.Report(new("5/8 · Search workers starting", $"CP-SAT model prepared; up to {seconds:0}s to {(allowPartial ? "maximise placed periods" : "find a complete timetable")}."));
            progress?.Report(new("5/8 · CP-SAT feasibility search", $"Searching all teacher, room and student constraints together; {seconds:0}s time limit. A percentage complete is unavailable until a feasible solution exists."));
            var status = solver.Solve(model);
            if (status is CpSolverStatus.Feasible or CpSolverStatus.Optimal)
                progress?.Report(new("5/8 · First feasible timetable found", $"{solver.WallTime():F1}s · {groups.Sum(g => g.Periods)} lesson-periods scheduled"));
            else
                progress?.Report(new("5/8 · Search ended without a timetable", $"Status {status} · {solver.WallTime():F1}s · {solver.NumConflicts()} conflicts · {solver.NumBranches()} branches"));

            progress?.Report(new("5/8 · Search completed", $"CP-SAT: {status} · {solver.WallTime():F1}s search · {solver.NumConflicts()} conflicts · {solver.NumBranches()} branches"));
            if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                return new(false, $"Solver status: {status}; search {solver.WallTime():F1}s, {solver.NumConflicts()} conflicts, {solver.NumBranches()} branches. No timetable was saved.", []);

            var output = new List<Placement>();
            for (int g = 0; g < groups.Count; g++)
            {
                for (int s = 0; s < slotCount; s++)
                {
                    if (solver.Value(used[g, s]) == 1)
                    {
                        int t = groups[g].TeacherIds.First(t => solver.Value(teacher[(g, s, t)]) == 1);
                        int r = groups[g].RoomIds.First(r => solver.Value(room[(g, s, r)]) == 1);
                        output.Add(new(g, (DayOfWeek)(s / periodsPerDay + 1), s % periodsPerDay + 1, t, r));
                    }
                }
            }

            return new(true, status.ToString(), output);
        }
    }
}