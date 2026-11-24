using Microsoft.EntityFrameworkCore;
using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Models;

namespace JollySchoolTimetabler.Services
{
    public sealed record TimetableProgress(string Stage, string Message);

    public class DiagnosticFailedClass
    {
        public Class Class { get; set; } = null!;
        public string Reason { get; set; } = string.Empty;
    }

    public class TimetableGenerationResult
    {
        public List<TimetableEntry> Entries { get; set; } = new();
        public List<Class> Classes { get; set; } = new();
        public List<DiagnosticFailedClass> FailedClasses { get; set; } = new();
        public string DiagnosticLog { get; set; } = "";
    }

    internal record Slot(DayOfWeek Day, int Period);

    // Retained for the database view model's existing timetable utilities.
    internal record TimetablePlacement(int ClassId, int TeacherId, int RoomId, int YearGroupId, List<Slot> Slots);

    public class TimetableGenerator
    {
        private readonly SchoolContext _context;

        // Configure these as needed
        public int PeriodsPerDay { get; set; } = 6;
        public int MaxClassSize { get; set; } = 30;

        public TimetableGenerator(SchoolContext context)
        {
            _context = context;
        }

        public async Task<TimetableGenerationResult> GenerateAndSaveAsync(IProgress<TimetableProgress>? progress = null)
        {
            var result = new TimetableGenerationResult();
            progress?.Report(new("1/8 · Loading school data", "Reading subjects, teachers, rooms, students and year groups"));
            var log = new List<string>();
            var prerequisiteErrors = new List<string>();
            // All preparation is read-only. A failed solve cannot change the existing timetable.
            var subjects = await _context.Subjects.AsNoTracking().ToDictionaryAsync(s => s.SubjectId);
            var teachers = await _context.Teachers.AsNoTracking().Include(t => t.Subjects).ToListAsync();
            var rooms = await _context.Rooms.AsNoTracking().ToListAsync();
            var students = await _context.Students.AsNoTracking().Include(s => s.Subjects).ToListAsync();
            var yearGroups = await _context.YearGroups.AsNoTracking().ToListAsync();
            if (subjects.Count == 0 || teachers.Count == 0 || rooms.Count == 0 || students.Count == 0)
            {
                result.DiagnosticLog = "Subjects, teachers, rooms and students must be configured first.";
                return result;
            }

            progress?.Report(new("2/8 · Building teaching groups", "Grouping students by year group and selected subject"));
            var curriculumRules = JollySchoolTimetabler.Services.Scheduling.CurriculumSettings.Load().ToDictionary(r => (r.YearGroupId, r.SubjectId));
            int Required(int yearId, int subjectId) => curriculumRules.TryGetValue((yearId, subjectId), out var r) ? r.WeeklyPeriods : subjects[subjectId].RequiredPeriods;
            var groups = new List<JollySchoolTimetabler.Services.Scheduling.TimetableSolver.Group>();
            int classNumber = 0;
            foreach (var yg in yearGroups.OrderBy(y => y.YearGroupId))
            {
                var yearStudents = students.Where(s => s.CurrentYearGroup == yg.YearGroupId).ToList();
                // Stable core cohorts: use identical membership across all seven
                // compulsory subjects instead of repartitioning each subject.
                var orderedYearIds = yearStudents.Select(s => s.StudentId).OrderBy(id => id).ToArray();
                var yearOptionCodes = JollySchoolTimetabler.Services.Scheduling.CurriculumSettings.OptionCodes;
                var yearOptions = subjects.Values.Where(s => yearOptionCodes.Contains(s.Code))
                    .OrderBy(s => s.SubjectId).ToArray();
                // Block scheduling requires exactly two complementary pathways of
                // four options, with every student taking one complete pathway.
                var pathA = yearOptions.Take(4).Select(s => s.SubjectId).ToHashSet();
                var pathB = yearOptions.Skip(4).Take(4).Select(s => s.SubjectId).ToHashSet();
                bool blockCompatible = yearOptions.Length == 8 && yearStudents.All(st =>
                {
                    var chosen = st.Subjects.Select(x => x.SubjectId).Where(id => pathA.Contains(id) || pathB.Contains(id)).ToHashSet();
                    return chosen.SetEquals(pathA) || chosen.SetEquals(pathB);
                });
                if (!blockCompatible)
                    log.Add($"Year {yg.YearGroupId}: option selections are not arranged into two complementary pathways; shared option blocks disabled. Reseed to generate coordinated options.");
                else
                    log.Add($"Year {yg.YearGroupId}: four coordinated option blocks enabled (two parallel pathways).");

                foreach (var subjectId in yearStudents.SelectMany(s => s.Subjects).Select(s => s.SubjectId).Distinct())
                {
                    if (!subjects.TryGetValue(subjectId, out var subject))
                    {
                        prerequisiteErrors.Add($"Unknown subject {subjectId} selected by students in year group {yg.YearGroupId}.");
                        continue;
                    }

                    if (Required(yg.YearGroupId, subjectId) <= 0 || Required(yg.YearGroupId, subjectId) > 5 * PeriodsPerDay)
                    {
                        prerequisiteErrors.Add($"Invalid weekly periods={Required(yg.YearGroupId, subjectId)} for {subject.Name} in year {yg.YearGroupId}.");
                        continue;
                    }

                    var ids = yearStudents.Where(s => s.Subjects.Any(sub => sub.SubjectId == subjectId)).Select(s => s.StudentId).OrderBy(id => id).ToArray();
                    if (ids.Length == 0)
                        continue;

                    curriculumRules.TryGetValue((yg.YearGroupId, subjectId), out var rule);
                    int preferred = rule?.PreferredClassSize ?? MaxClassSize;
                    int maximum = rule?.MaximumClassSize ?? 35;
                    var eligibleTeachers = teachers.Where(t => t.Subjects.Any(s => s.SubjectId == subjectId)).Select(t => t.TeacherId).Distinct().ToArray();
                    int largestRoom = rooms.Count == 0 ? 0 : rooms.Max(r => r.Capacity);
                    int effectiveMaximum = Math.Min(maximum, largestRoom);
                    if (eligibleTeachers.Length == 0)
                    {
                        prerequisiteErrors.Add($"No qualified teacher: year {yg.YearGroupId} / {subject.Name} ({ids.Length} students).");
                        continue;
                    }

                    if (effectiveMaximum <= 0)
                    {
                        prerequisiteErrors.Add($"No suitable room: year {yg.YearGroupId} / {subject.Name}; largest room capacity {largestRoom}.");
                        continue;
                    }

                    int groupCount = JollySchoolTimetabler.Services.Scheduling.CurriculumSettings.GroupCount(ids.Length, Math.Min(preferred, effectiveMaximum), effectiveMaximum);
                    var groupSizes = Enumerable.Range(0, groupCount).Select(i => (i + 1) * ids.Length / groupCount - i * ids.Length / groupCount).ToArray();
                    log.Add($"Year {yg.YearGroupId} / {subject.Name}: {ids.Length} students, {groupCount} groups ({string.Join(", ", groupSizes)}); room limit {effectiveMaximum}, qualified teachers {eligibleTeachers.Length}.");
                    for (int i = 0; i < groupCount; i++)
                    {
                        int start = i * ids.Length / groupCount;
                        int end = (i + 1) * ids.Length / groupCount;
                        var members = ids.Skip(start).Take(end - start).ToArray();
                        var eligibleRooms = rooms.Where(r => r.Capacity >= members.Length).Select(r => r.RoomId).Distinct().ToArray();
                        var cls = new Class
                        {
                            SubjectId = subjectId, YearGroupId = yg.YearGroupId,
                            Name = $"YG{yg.YearGroupId}-{subject.Name}-G{++classNumber}",
                            StudentIds = members.ToList()
                        };

                        int optionBlock = -1;
                        if (blockCompatible && Required(yg.YearGroupId, subjectId) == 1)
                        {
                            int optionIndex = Array.FindIndex(yearOptions, x => x.SubjectId == subjectId);
                            if (optionIndex >= 0)
                                optionBlock = optionIndex % 4;
                        }
                        groups.Add(new(groups.Count, cls, members, Required(yg.YearGroupId, subjectId), rule?.DoublePeriods ?? subject.RequiresDoublePeriod, eligibleTeachers, eligibleRooms, optionBlock));
                        if (eligibleRooms.Length == 0)
                            prerequisiteErrors.Add($"No room for {cls.Name} ({members.Length} students; largest room {largestRoom}).");
                    }
                }
            }

            foreach (var student in students)
            {
                int workload = student.Subjects.Sum(s => Required(student.CurrentYearGroup, s.SubjectId));
                if (workload > 5 * PeriodsPerDay)
                    prerequisiteErrors.Add($"Student {student.StudentId} requires {workload} weekly periods, but only {5 * PeriodsPerDay} exist.");

                foreach (var compulsory in curriculumRules.Values.Where(r => r.YearGroupId == student.CurrentYearGroup && r.Compulsory))
                {
                    if (!student.Subjects.Any(s => s.SubjectId == compulsory.SubjectId))
                        prerequisiteErrors.Add($"Student {student.StudentId} is missing compulsory subject {subjects.GetValueOrDefault(compulsory.SubjectId)?.Name ?? compulsory.SubjectId.ToString()}.");
                }
            }

            // Curriculum settings are authoritative after reseeding. Validate the
            // student's actual workload instead of forcing the original seed defaults.
            var coreCodes = JollySchoolTimetabler.Services.Scheduling.CurriculumSettings.CoreCodes;
            var optionCodes = JollySchoolTimetabler.Services.Scheduling.CurriculumSettings.OptionCodes;
            var subjectByCode = subjects.Values.Where(s => s.Code != null).GroupBy(s => s.Code!).ToDictionary(g => g.Key, g => g.First());
            if (coreCodes.All(subjectByCode.ContainsKey) && optionCodes.All(subjectByCode.ContainsKey))
            {
                foreach (var year in yearGroups)
                {
                    foreach (var code in coreCodes)
                    {
                        var subject = subjectByCode[code];
                        if (curriculumRules.TryGetValue((year.YearGroupId, subject.SubjectId), out var rule) && !rule.Compulsory)
                            prerequisiteErrors.Add($"Year {year.YearGroupId} / {subject.Name}: core subject must be compulsory.");
                    }

                    foreach (var code in optionCodes)
                    {
                        var subject = subjectByCode[code];
                        if (Required(year.YearGroupId, subject.SubjectId) != 1)
                            prerequisiteErrors.Add($"Year {year.YearGroupId} / {subject.Name}: optional subject must have one weekly period.");
                    }
                }

                foreach (var student in students)
                {
                    var codes = student.Subjects.Select(s => s.Code).ToHashSet();
                    var missing = coreCodes.Where(c => !codes.Contains(c)).ToArray();
                    int options = optionCodes.Count(c => codes.Contains(c));
                    if (missing.Length != 0 || options != 4 || student.Subjects.Count != 11)
                        prerequisiteErrors.Add($"Student {student.StudentId}: missing core [{string.Join(", ", missing)}], {options}/4 options, {student.Subjects.Count}/11 total subjects.");

                    int periods = student.Subjects.Sum(s => Required(student.CurrentYearGroup, s.SubjectId));
                    if (periods != 5 * PeriodsPerDay)
                        prerequisiteErrors.Add($"Student {student.StudentId}: {periods}/{5 * PeriodsPerDay} required weekly periods. Adjust curriculum before generating.");
                }
            }

            log.Insert(0, $"Built {groups.Count} teaching groups; {groups.Sum(g => g.Periods)} required periods.");
            progress?.Report(new("3/8 · Checking prerequisites", $"{groups.Count} groups, {groups.Sum(g => g.Periods)} required periods; checking teacher and room eligibility"));
            if (groups.Count == 0 || prerequisiteErrors.Count > 0)
            {
                log.Add($"Prerequisite validation failed: {prerequisiteErrors.Count} error(s). CP-SAT was not started.");
                log.AddRange(prerequisiteErrors);
                result.DiagnosticLog = string.Join(Environment.NewLine, log);
                return result;
            }

            progress?.Report(new("3/8 · Analysing constraints", "Checking teacher pools, room pools and student workloads"));
            var constraintReport = JollySchoolTimetabler.Services.Scheduling.TimetableConstraintDiagnostics.Analyse(groups, PeriodsPerDay);
            progress?.Report(new("Resource concerns", constraintReport.Errors.Count > 0 ? string.Join(" | ", constraintReport.Errors.Take(4)) : constraintReport.Risks.Count > 0 ? string.Join(" | ", constraintReport.Risks.Take(5)) : "No immediate capacity shortages detected; CP-SAT may still encounter scheduling conflicts."));
            log.AddRange(constraintReport.Lines);
            if (constraintReport.Errors.Count > 0)
            {
                log.Add("Capacity analysis found proven contradictions. CP-SAT was not started.");
                result.DiagnosticLog = string.Join(Environment.NewLine, log);
                return result;
            }

            log.Add("Prerequisite validation passed. Starting CP-SAT solver.");
            progress?.Report(new("4/8 · Preparing solver", $"Preparing {groups.Count} teaching groups and {groups.Sum(g => g.Periods)} required periods"));
            var solver = new JollySchoolTimetabler.Services.Scheduling.TimetableSolver();

            // The solver is CPU intensive; never run it on the WPF dispatcher thread.
            var solution = await Task.Run(() => solver.Solve(groups, PeriodsPerDay, progress: progress));
            log.Add($"CP-SAT: {solution.Status}; placed {solution.Placements.Count}/{groups.Sum(g => g.Periods)} periods.");
            if (!solution.Success)
            {
                // Diagnostic-only relaxation: never persist an incomplete timetable.
                progress?.Report(new("5/8 · Partial timetable diagnostics", "Complete feasibility search failed; finding the largest conflict-free partial placement."));
                var partial = await Task.Run(() => solver.Solve(groups, PeriodsPerDay, seconds: 45, progress: progress, allowPartial: true));
                if (partial.Success)
                {
                    int required = groups.Sum(g => g.Periods);
                    log.Add($"PARTIAL DIAGNOSTIC ONLY: {partial.Placements.Count}/{required} lesson-periods placed; {required - partial.Placements.Count} unplaced. Nothing was saved.");
                    foreach (var group in groups)
                    {
                        int placed = partial.Placements.Count(p => p.GroupIndex == group.Index);
                        if (placed < group.Periods)
                            log.Add($"UNSCHEDULED: {group.TeachingClass.Name} (group {group.Index + 1}): {group.Periods - placed}/{group.Periods} missing; {group.StudentIds.Length} students; {group.TeacherIds.Length} eligible teachers; {group.RoomIds.Length} eligible rooms.");
                    }
                    log.Add("The partial solution is diagnostic, not proof of which constraint causes infeasibility. Try adjusting workload, teacher availability or room allocation.");
                }
                else
                    log.Add($"Partial diagnostic search also ended without a solution: {partial.Status}");

                result.DiagnosticLog = string.Join(Environment.NewLine, log);
                return result;
            }

            progress?.Report(new("6/8 · Validating timetable", $"Checking {solution.Placements.Count} placements for student, teacher and room conflicts"));
            var validator = new JollySchoolTimetabler.Services.Scheduling.TimetableValidator();
            var errors = validator.Validate(groups, solution.Placements, PeriodsPerDay);
            if (errors.Count != 0)
            {
                result.DiagnosticLog = string.Join(Environment.NewLine, log.Concat(errors));
                return result;
            }

            progress?.Report(new("7/8 · Auditing allocations", "Checking every student receives their required subject periods"));

            // Independent weekly allocation audit: every selected student/subject must receive
            // exactly the configured number of periods, not merely one lesson per group.
            var studentAllocations = new Dictionary<(int StudentId, int SubjectId), int>();
            foreach (var placement in solution.Placements)
            {
                var group = groups[placement.GroupIndex];
                foreach (int studentId in group.StudentIds)
                {
                    var key = (studentId, group.TeachingClass.SubjectId);
                    studentAllocations[key] = studentAllocations.GetValueOrDefault(key) + 1;
                }
            }

            var allocationErrors = new List<string>();
            foreach (var student in students)
            {
                foreach (var subject in student.Subjects)
                {
                    int actual = studentAllocations.GetValueOrDefault((student.StudentId, subject.SubjectId));
                    int expectedPeriods = Required(student.CurrentYearGroup, subject.SubjectId);
                    if (actual != expectedPeriods)
                        allocationErrors.Add($"Student {student.StudentId}, {subject.Name}: required {expectedPeriods}, scheduled {actual}.");
                }
            }

            if (allocationErrors.Count != 0)
            {
                result.DiagnosticLog = string.Join(Environment.NewLine, log.Concat(allocationErrors));
                return result;
            }

            log.Add("Weekly subject allocation audit: all student selections match required periods.");
            foreach (var yg in yearGroups.OrderBy(y => y.YearGroupId))
            {
                var yearGroupsForReport = groups.Where(g => g.TeachingClass.YearGroupId == yg.YearGroupId);
                foreach (var subjectGroups in yearGroupsForReport.GroupBy(g => g.TeachingClass.SubjectId).OrderBy(g => subjects[g.Key].Name))
                {
                    var subject = subjects[subjectGroups.Key];
                    int expected = subjectGroups.Sum(g => g.Periods);
                    int actual = solution.Placements.Count(p => subjectGroups.Any(g => g.Index == p.GroupIndex));
                    log.Add($"Year group {yg.YearGroupId} / {subject.Name}: {actual}/{expected} group-periods; {Required(yg.YearGroupId, subject.SubjectId)} per student per week.");
                }
            }

            progress?.Report(new("8/8 · Saving timetable", $"Saving {solution.Placements.Count} validated timetable entries"));

            // Existing databases were created before the many-to-many membership table
            // existed. EnsureCreated() does not add tables to an existing database.
            // Create the new join table idempotently before beginning the transaction.
            if (_context.Database.IsSqlite())
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS StudentClassMembership (
                        ClassId INTEGER NOT NULL,
                        StudentId INTEGER NOT NULL,
                        PRIMARY KEY (ClassId, StudentId),
                        FOREIGN KEY (ClassId) REFERENCES Classes(ClassId) ON DELETE CASCADE,
                        FOREIGN KEY (StudentId) REFERENCES Students(StudentId) ON DELETE CASCADE
                    );");

                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE INDEX IF NOT EXISTS IX_StudentClassMembership_StudentId
                    ON StudentClassMembership(StudentId);");
            }
            else if (_context.Database.IsSqlServer())
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID(N'dbo.StudentClassMembership', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.StudentClassMembership (
                            ClassId int NOT NULL,
                            StudentId int NOT NULL,
                            CONSTRAINT PK_StudentClassMembership PRIMARY KEY (ClassId, StudentId),
                            CONSTRAINT FK_SCM_Class FOREIGN KEY (ClassId) REFERENCES dbo.Classes(ClassId) ON DELETE CASCADE,
                            CONSTRAINT FK_SCM_Student FOREIGN KEY (StudentId) REFERENCES dbo.Students(StudentId) ON DELETE CASCADE
                        );
                        CREATE INDEX IX_StudentClassMembership_StudentId ON dbo.StudentClassMembership(StudentId);
                    END;");
            }

            progress?.Report(new("8/8 · Saving timetable", $"Saving {solution.Placements.Count} validated timetable entries"));
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove old classes and memberships only after a complete solution exists.
                var oldEntries = await _context.TimetableEntries.ToListAsync();
                _context.TimetableEntries.RemoveRange(oldEntries);
                await _context.SaveChangesAsync();
                var oldClasses = await _context.Classes.Include(c => c.Students).ToListAsync();
                foreach (var oldClass in oldClasses) oldClass.Students.Clear();
                await _context.SaveChangesAsync();
                _context.Classes.RemoveRange(oldClasses);
                await _context.SaveChangesAsync();

                var studentIds = groups.SelectMany(g => g.StudentIds).Distinct().ToArray();
                var trackedStudents = await _context.Students.Where(s => studentIds.Contains(s.StudentId)).ToDictionaryAsync(s => s.StudentId);
                foreach (var group in groups)
                {
                    var cls = group.TeachingClass;
                    var first = solution.Placements.First(p => p.GroupIndex == group.Index);
                    cls.TeacherId = first.TeacherId;
                    cls.RoomId = first.RoomId;
                    cls.DayOfWeek = first.Day;
                    cls.Period = first.Period;
                    cls.Students = group.StudentIds.Select(id => trackedStudents[id]).ToList();
                    _context.Classes.Add(cls);
                }

                await _context.SaveChangesAsync();
                var entries = solution.Placements.Select(p =>
                {
                    var cls = groups[p.GroupIndex].TeachingClass;
                    return new TimetableEntry
                    {
                        ClassId = cls.ClassId, StudentId = 0, SubjectId = cls.SubjectId,
                        TeacherId = p.TeacherId, RoomId = p.RoomId, YearGroupId = cls.YearGroupId,
                        DayOfWeek = p.Day, Period = p.Period
                    };
                }).ToList();

                _context.TimetableEntries.AddRange(entries);
                await _context.SaveChangesAsync();

                // Verify the actual persisted join table and timetable entries before committing.
                // This catches the failure mode where lessons exist but a student has only one class.
                _context.ChangeTracker.Clear();
                var savedClasses = await _context.Classes.AsNoTracking().Include(c => c.Students).ToListAsync();
                var savedEntries = await _context.TimetableEntries.AsNoTracking().ToListAsync();
                var savedMembers = savedClasses.ToDictionary(c => c.ClassId, c => c.Students.Select(st => st.StudentId).ToHashSet());
                var persistedPeriods = new Dictionary<(int StudentId, int SubjectId), int>();
                foreach (var entry in savedEntries)
                {
                    if (!entry.ClassId.HasValue || !savedMembers.TryGetValue(entry.ClassId.Value, out var memberIds))
                        throw new InvalidOperationException($"Saved timetable entry has no valid teaching class: {entry.ClassId}.");

                    foreach (var studentId in memberIds)
                    {
                        var key = (studentId, entry.SubjectId);
                        persistedPeriods[key] = persistedPeriods.GetValueOrDefault(key) + 1;
                    }
                }

                var persistenceErrors = new List<string>();
                if (savedEntries.Count != solution.Placements.Count)
                    persistenceErrors.Add($"Saved entries: {savedEntries.Count}; expected {solution.Placements.Count}.");

                foreach (var student in students)
                {
                    foreach (var subject in student.Subjects)
                    {
                        int expected = Required(student.CurrentYearGroup, subject.SubjectId);
                        int actual = persistedPeriods.GetValueOrDefault((student.StudentId, subject.SubjectId));
                        if (actual != expected)
                            persistenceErrors.Add($"Student {student.StudentId} / {subject.Name}: saved {actual}, expected {expected}.");
                    }
                }

                if (persistenceErrors.Count > 0)
                    throw new InvalidOperationException("Saved timetable membership audit failed; changes rolled back. " + string.Join(" | ", persistenceErrors.Take(15)));

                await transaction.CommitAsync();
                log.Add($"Post-save audit passed: {savedEntries.Count} entries, {savedClasses.Count} classes, all student subject allocations verified.");
                result.Classes = groups.Select(g => g.TeachingClass).ToList();
                result.Entries = entries;
                log.Add($"Validated and saved {entries.Count} timetable entries.");
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();
                throw;
            }

            result.DiagnosticLog = string.Join(Environment.NewLine, log);
            progress?.Report(new("Complete", $"Saved {result.Entries.Count} validated timetable entries"));
            return result;
        }
    }
}
