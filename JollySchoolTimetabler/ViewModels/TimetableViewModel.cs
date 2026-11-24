using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Events;
using JollySchoolTimetabler.Enums;
using JollySchoolTimetabler.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using JollySchoolTimetabler.Utils;

namespace JollySchoolTimetabler.ViewModels
{
    public class Period
    {
        public int Number { get; set; }
        public string TimeRange { get; set; } = string.Empty;
    }

    public class TimetableViewModel : ViewModelBase
    {
        static TimetableViewModel _instance = null;

        private readonly SchoolContext _context;

        public RelayCommand ReloadCommand { get; }
        public ObservableCollection<TimetableEntry> TimetableEntries { get; set; } = new();
        public ObservableCollection<DayOfWeek> Days { get; } = new(Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Where(d => d >= DayOfWeek.Monday && d <= DayOfWeek.Friday));

        private TimetableMode _currentMode;
        public TimetableMode CurrentMode
        {
            get => _currentMode;
            set 
            { 
                _currentMode = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModeDisplayName));
                LoadTimetable(); 
            }
        }

        public string ModeDisplayName => SelectedEntity switch
        {
            Student s when CurrentMode == TimetableMode.Student => $"Student: {s.FirstName} {s.LastName}",
            Teacher t when CurrentMode == TimetableMode.Teacher => $"Teacher: {t.FullName}",
            Room r when CurrentMode == TimetableMode.Room => $"Room: {r.Name}",
            Class c when CurrentMode == TimetableMode.Class => $"Class: {c.Name}",
            _ => $"{CurrentMode}: No selection"
        };

        public string TimetableSummary { get; private set; } = "Select a student, teacher, class or room.";

        private Student? _selectedStudent;
        public Student? SelectedStudent
        {
            get => _selectedStudent;
            set { _selectedStudent = value; OnPropertyChanged(); if (value != null) SetSelectedEntity(value); }
        }

        private Teacher? _selectedTeacher;
        public Teacher? SelectedTeacher
        {
            get => _selectedTeacher;
            set { _selectedTeacher = value; OnPropertyChanged(); if (value != null) SetSelectedEntity(value); }
        }

        private Room? _selectedRoom;
        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (value == null || _selectedRoom?.RoomId == value.RoomId) return;
                SetSelectedEntity(value);
            }
        }

        private object? _selectedEntity;
        public object? SelectedEntity
        {
            get => _selectedEntity;
            set 
            { 
                if (value is Room room) { SetSelectedEntity(room); return; }
                _selectedEntity = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModeDisplayName));
                LoadTimetable();
            }
        }

        public ObservableCollection<Period> Periods { get; } = new()
        {
            new Period { Number = 1, TimeRange = "09:00 - 09:50" },
            new Period { Number = 2, TimeRange = "09:55 - 10:45" },
            new Period { Number = 3, TimeRange = "10:50 - 11:40" },
            new Period { Number = 4, TimeRange = "11:45 - 12:35" },
            new Period { Number = 5, TimeRange = "13:30 - 14:20" },
            new Period { Number = 6, TimeRange = "14:25 - 15:15" }
        };

        public ObservableCollection<Student> Students { get; set; } = new();
        public ObservableCollection<Teacher> Teachers { get; set; } = new();
        public ObservableCollection<Room> Rooms { get; set; } = new();
        public ObservableCollection<Subject> Subjects { get; set; } = new();
        public ObservableCollection<PeriodCell> PeriodCells { get; } = new();

        // Local cache lookups for fast mapping
        private List<Subject> _subjects = new();
        private List<Room> _rooms = new();
        private List<YearGroup> _yearGroups = new();

        public int DaysCount => Days?.Count ?? 0;
        public int PeriodsCount => Periods?.Count ?? 0;
        
        static public TimetableViewModel getInstance()
        {
            if (_instance == null)
            {
                _instance = new TimetableViewModel(App.DbContext);
            }

            return _instance;
        }

        public TimetableViewModel(SchoolContext context)
        {
            _context = context;
            ReloadCommand = new RelayCommand(RefreshTimetable);

            // Load local caches
            _subjects = _context.Subjects.AsNoTracking().ToList();
            _rooms = _context.Rooms.AsNoTracking().ToList();
            _yearGroups = _context.YearGroups.AsNoTracking().ToList();

            Subjects = new ObservableCollection<Subject>(_subjects);

            LoadStudents();
            LoadTeachers();
            LoadRooms();
            BuildPeriodCells();
            ReloadPeople();
            AppEvents.DataChanged += RefreshReferenceData;
            AppEvents.DatabaseReset += RefreshReferenceData;

            CurrentMode = TimetableMode.Student;
            _instance = this;
            var savedCount = _context.TimetableEntries.AsNoTracking().Count();
            TimetableSummary = savedCount == 0
                ? "No saved timetable. Generate a complete timetable first."
                : $"{savedCount} saved lessons loaded. Select a student or teacher.";
        }

        public void SetSelectedEntity(object entity)
        {
            // The list tabs and the selectors in the timetable must refer to the same selection.
            // Set mode before entity to avoid querying with a mismatched mode.
            var mode = entity switch
            {
                Student => TimetableMode.Student,
                Teacher => TimetableMode.Teacher,
                Room => TimetableMode.Room,
                Class => TimetableMode.Class,
                _ => CurrentMode
            };
            if (_currentMode != mode)
            {
                _currentMode = mode;
                OnPropertyChanged(nameof(CurrentMode));
            }
            _selectedEntity = entity;
            OnPropertyChanged(nameof(SelectedEntity));
            if (entity is Room room)
            {
                _selectedRoom = Rooms.FirstOrDefault(r => r.RoomId == room.RoomId) ?? room;
                OnPropertyChanged(nameof(SelectedRoom));
            }
            if (entity is Student student)
            {
                _selectedStudent = Students.FirstOrDefault(s => s.StudentId == student.StudentId) ?? student;
                OnPropertyChanged(nameof(SelectedStudent));
            }
            if (entity is Teacher teacher)
            {
                _selectedTeacher = Teachers.FirstOrDefault(t => t.TeacherId == teacher.TeacherId) ?? teacher;
                OnPropertyChanged(nameof(SelectedTeacher));
            }
            OnPropertyChanged(nameof(ModeDisplayName));
            LoadTimetable();
        }

        private async void LoadStudents()
        {
            var students = await _context.Students
                .Include(s => s.Subjects)
                .AsNoTracking()
                .ToListAsync();

            Students.Clear();
            foreach (var s in students)
                Students.Add(s);
        }

        private async void LoadTeachers()
        {
            var teachers = await _context.Teachers
                .Include(t => t.Subjects)
                .AsNoTracking()
                .ToListAsync();

            Teachers.Clear();
            foreach (var t in teachers)
                Teachers.Add(t);
        }

        private async void LoadRooms()
        {
            var rooms = await _context.Rooms.ToListAsync();
            Rooms.Clear();
            foreach (var r in rooms)
                Rooms.Add(r);
        }

        private void RefreshReferenceData()
        {
            _subjects = _context.Subjects.AsNoTracking().ToList();
            _rooms = _context.Rooms.AsNoTracking().ToList();
            _yearGroups = _context.YearGroups.AsNoTracking().ToList();
            Subjects = new ObservableCollection<Subject>(_subjects);
            OnPropertyChanged(nameof(Subjects));
            Teachers = new ObservableCollection<Teacher>(_context.Teachers.AsNoTracking().ToList());
            Rooms = new ObservableCollection<Room>(_rooms);
            OnPropertyChanged(nameof(Teachers));
            OnPropertyChanged(nameof(Rooms));
            if (_selectedRoom != null)
            {
                _selectedRoom = Rooms.FirstOrDefault(r => r.RoomId == _selectedRoom.RoomId);
                OnPropertyChanged(nameof(SelectedRoom));
            }
            ReloadPeople();
            LoadTimetable();
        }

        public void RefreshTimetable()
        {
            // Reload from the persisted database, including after application restart.
            _context.ChangeTracker.Clear();
            LoadTimetable();
        }

        public void ReloadPeople()
        {
            Students = new ObservableCollection<Student>(_context.Students.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToList());
            Teachers = new ObservableCollection<Teacher>(_context.Teachers.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToList());
            OnPropertyChanged(nameof(Students));
            OnPropertyChanged(nameof(Teachers));
            if (_selectedEntity is Student st)
            {
                _selectedStudent = Students.FirstOrDefault(x => x.StudentId == st.StudentId);
                OnPropertyChanged(nameof(SelectedStudent));
            }
            if (_selectedEntity is Teacher tc)
            {
                _selectedTeacher = Teachers.FirstOrDefault(x => x.TeacherId == tc.TeacherId);
                OnPropertyChanged(nameof(SelectedTeacher));
            }
        }

        private void LoadTimetable()
        {
            TimetableEntries.Clear();
            OnPropertyChanged(nameof(TimetableEntries));

            if (SelectedEntity == null)
            {
                TimetableSummary = "Select a student, teacher, class or room.";
                OnPropertyChanged(nameof(TimetableSummary));
                RefreshPeriodCells();
                return;
            }

            IQueryable<TimetableEntry> query = _context.TimetableEntries.AsNoTracking();

            if ((CurrentMode == TimetableMode.Student && SelectedEntity is not Student) ||
                (CurrentMode == TimetableMode.Teacher && SelectedEntity is not Teacher) ||
                (CurrentMode == TimetableMode.Class && SelectedEntity is not Class) ||
                (CurrentMode == TimetableMode.Room && SelectedEntity is not Room)) { RefreshPeriodCells(); return; }

            switch (CurrentMode)
            {
                case TimetableMode.Student:
                    if (SelectedEntity is Student s)
                    {
                        // Resolve memberships separately: timetable entries are stored per class,
                        // with StudentId=0 for shared teaching-group lessons.
                        var classIds = _context.Classes.AsNoTracking()
                            .Where(c => c.Students.Any(st => st.StudentId == s.StudentId))
                            .Select(c => c.ClassId).ToArray();
                        query = query.Where(e => e.StudentId == s.StudentId ||
                            (e.ClassId.HasValue && classIds.Contains(e.ClassId.Value)));
                    }
                    break;

                case TimetableMode.Teacher:
                    if (SelectedEntity is Teacher t)
                        query = query.Where(e => e.TeacherId == t.TeacherId);
                    break;

                case TimetableMode.Class:
                    if (SelectedEntity is Class c)
                        query = query.Where(e => e.ClassId == c.ClassId);
                    break;

                case TimetableMode.Room:
                    if (SelectedEntity is Room r)
                        query = query.Where(e => e.RoomId == r.RoomId);
                    break;
            }

            var found = query.OrderBy(e => e.DayOfWeek).ThenBy(e => e.Period).ToList();
            foreach (var e in found)
                TimetableEntries.Add(e);

            var uniqueSlots = found.Select(e => (e.DayOfWeek, e.Period)).Distinct().Count();
            int totalSaved = _context.TimetableEntries.AsNoTracking().Count();
            TimetableSummary = $"{found.Count} saved lessons · {uniqueSlots} occupied periods · {30 - uniqueSlots} free periods · {totalSaved} school-wide";
            if (CurrentMode == TimetableMode.Student && SelectedEntity is Student student)
            {
                var subjectIds = _context.Students.AsNoTracking()
                    .Where(s => s.StudentId == student.StudentId)
                    .SelectMany(s => s.Subjects).Select(s => s.SubjectId).ToArray();
                var actualSubjects = found.Select(e => e.SubjectId).Distinct().Count();
                TimetableSummary += $" · {actualSubjects}/{subjectIds.Length} subjects represented";
                if (subjectIds.Length > actualSubjects)
                    TimetableSummary += $" · WARNING: {subjectIds.Length - actualSubjects} selected subject(s) have no saved lessons";
                if (totalSaved == 0)
                    TimetableSummary += " · No complete timetable has been saved yet";
            }
            OnPropertyChanged(nameof(TimetableSummary));
            RefreshPeriodCells();
            OnPropertyChanged(nameof(TimetableEntries));
        }

        private void RefreshPeriodCells()
        {
            var subjects = _subjects.ToDictionary(x => x.SubjectId, x => x.Name);
            var teachers = _context.Teachers.AsNoTracking().ToDictionary(x => x.TeacherId, x => x.FullName);
            var rooms = _rooms.ToDictionary(x => x.RoomId, x => x.Name);
            foreach (var cell in PeriodCells)
            {
                var lessons = TimetableEntries.Where(e => e.DayOfWeek == cell.Day && e.Period == cell.Period)
                    .Select(e =>
                    {
                        var subject = subjects.GetValueOrDefault(e.SubjectId, "Unknown subject");
                        var teacher = teachers.GetValueOrDefault(e.TeacherId, "Unknown teacher");
                        var room = rooms.GetValueOrDefault(e.RoomId, "Unknown room");
                        return CurrentMode switch
                        {
                            TimetableMode.Teacher => $"{subject}\n{room}",
                            TimetableMode.Room => $"{subject}\n{teacher}",
                            _ => $"{subject}\n{teacher}\n{room}"
                        };
                    }).Distinct().ToList();
                cell.DisplayText = lessons.Count == 0 ? "Free" : string.Join("\n────────\n", lessons);
            }
            // Replace the cell collection contents to reliably refresh WPF bindings.
            var snapshot = PeriodCells.Select(c => new PeriodCell { Day = c.Day, Period = c.Period, DisplayText = c.DisplayText }).ToArray();
            PeriodCells.Clear();
            foreach (var cell in snapshot) PeriodCells.Add(cell);
        }

        private void BuildPeriodCells()
        {
            PeriodCells.Clear();

            // Days and Periods already exist on your VM
            var days = Days.ToList();            // DayOfWeek Monday..Friday
            var periods = Periods.Select(p => p.Number).ToList(); // 1..6

            // Order by period then day (so UniformGrid Rows=6, Columns=5 will fill row by row)
            foreach (var period in periods)
            {
                foreach (var day in days)
                {
                    PeriodCells.Add(new PeriodCell { Day = day, Period = period });
                }
            }

            // Raise changed if you ever replace the collection (not necessary here)
            OnPropertyChanged(nameof(PeriodCells));
        }
        // Helper lookup for display
        public string GetSubjectName(int subjectId) => _subjects.FirstOrDefault(s => s.SubjectId == subjectId)?.Name ?? "Unknown";

        public string GetRoomName(int roomId) => _rooms.FirstOrDefault(r => r.RoomId == roomId)?.Name ?? "Unknown";

        public string GetYearGroupName(int yearGroupId) => _yearGroups.FirstOrDefault(y => y.YearGroupId == yearGroupId)?.Name ?? "";
    }
}
