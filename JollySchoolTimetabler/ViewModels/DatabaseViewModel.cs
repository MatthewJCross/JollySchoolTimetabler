using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Events;
using JollySchoolTimetabler.Models;
using JollySchoolTimetabler.Services;
using JollySchoolTimetabler.Utils;
using JollySchoolTimetabler.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;

namespace JollySchoolTimetabler.ViewModels
{
    public class DatabaseViewModel : ViewModelBase
    {
        private readonly SchoolContext _context;
        public ObservableCollection<YearGroup> YearGroups { get; set; } = new();
        public ObservableCollection<Student> Students { get; set; } = new ();
        public ObservableCollection<Subject> Subjects { get; set; } = new();
        public ObservableCollection<Teacher> Teachers { get; set; } = new ();
        public ObservableCollection<Room> Rooms { get; set; } = new();


        private bool _iconClickable = false;
        public bool IconClickable
        {
            get => _iconClickable;
            set
            {
                _iconClickable = value;
                OnPropertyChanged(nameof(IconClickable));
            }
        }

        private bool _isGenerating;
        public bool IsGenerating
        {
            get => _isGenerating;
            private set
            {
                _isGenerating = value;
                OnPropertyChanged();
                GenerateTimetableCommand?.RaiseCanExecuteChanged();
                ResetDatabaseCommand?.RaiseCanExecuteChanged();
                ConfigureCurriculumCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _generationPhase = "Ready";
        public string GenerationPhase
        {
            get => _generationPhase;
            private set 
            {
                _generationPhase = value; 
                OnPropertyChanged(); 
            }
        }

        private string _generationDetail = "";
        public string GenerationDetail
        {
            get => _generationDetail;
            private set
            {
                _generationDetail = value;
                OnPropertyChanged(); 
            }
        }

        private string _generationStatus = "";
        public string GenerationStatus
        {
            get => _generationStatus;
            private set 
            { 
                _generationStatus = value; 
                OnPropertyChanged(); 
            }
        }

        private string _generationElapsed = "00:00";
        public string GenerationElapsed
        {
            get => _generationElapsed;
            private set
            { 
                _generationElapsed = value;
                OnPropertyChanged(); 
            }
        }

        private string _resourceConcerns = "Run generation to analyse resource pressure.";
        public string ResourceConcerns
        {
            get => _resourceConcerns;
            private set 
            { 
                _resourceConcerns = value;
                OnPropertyChanged(); 
            }
        }

        private string _solverActivity = "Waiting for generation.";
        public string SolverActivity
        {
            get => _solverActivity;
            private set
            {
                _solverActivity = value;
                OnPropertyChanged(); 
            }
        }

        private string _lastGenerationDiagnostics = "";
        public RelayCommand ShowGenerationDiagnosticsCommand { get; }
        private readonly Stopwatch _generationWatch = new();
        private readonly DispatcherTimer _generationTimer = new() { Interval = TimeSpan.FromSeconds(1) };

        public RelayCommand ResetDatabaseCommand { get; }
        public RelayCommand GenerateTimetableCommand { get; }
        public RelayCommand ConfigureCurriculumCommand { get; }
        public TimetableViewModel Timetable { get; set; }

        public DatabaseViewModel()
        {
            ResetDatabaseCommand = new RelayCommand(ResetDatabase, () => !IsGenerating);
            GenerateTimetableCommand = new RelayCommand(GenerateTimetable, () => !IsGenerating);
            ConfigureCurriculumCommand = new RelayCommand(() => new CurriculumWindow { Owner = Application.Current?.MainWindow }.ShowDialog(), () => !IsGenerating);
            ShowGenerationDiagnosticsCommand = new RelayCommand(ShowGenerationDiagnostics);
            _generationTimer.Tick += (_, _) => GenerationElapsed = _generationWatch.Elapsed.ToString(@"mm\:ss");
            Timetable = TimetableViewModel.getInstance();
            _context = App.DbContext;
        }

        private void ResetDatabase()
        {
            try
            {
                using (var context = new SchoolContext())
                {
                    SeedData.Initialize(context);
                }

                MessageBox.Show("Database has been successfully reset and reseeded.", "Database Reset", MessageBoxButton.OK, MessageBoxImage.Information);

                // Notify all other ViewModels
                AppEvents.RaiseDatabaseReset();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting database:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        readonly int MAX_CLASS_SIZE = 30;
        private static readonly string[] separator = new[] { "\r\n", "\n" };

        private async void GenerateTimetable()
        {
            if (IsGenerating) return;
            IsGenerating = true;
            GenerationPhase = "1/8 · Loading school data";
            GenerationDetail = "Reading curriculum, teachers, rooms and students";
            GenerationStatus = "Loading school data...";
            ResourceConcerns = "Analysing teacher, room and student capacity...";
            SolverActivity = "Waiting for CP-SAT model preparation.";
            GenerationElapsed = "00:00";
            _generationWatch.Restart();
            _generationTimer.Start();
            try
            {
                // Allow WPF to render the indicator before database preparation begins.
                await Dispatcher.Yield(DispatcherPriority.Background);
                var generator = new TimetableGenerator(_context);
                var progress = new Progress<TimetableProgress>(p =>
                {
                    GenerationPhase = p.Stage;
                    GenerationDetail = p.Message;
                    GenerationStatus = $"{p.Stage}: {p.Message}";
                    if (p.Stage.Contains("Resource concerns")) 
                        ResourceConcerns = p.Message;

                    if (p.Stage.Contains("solver", StringComparison.OrdinalIgnoreCase) || p.Stage.Contains("Search", StringComparison.OrdinalIgnoreCase) || p.Stage.Contains("timetable", StringComparison.OrdinalIgnoreCase))
                        SolverActivity = $"{p.Stage} — {p.Message}";
                });

                var result = await generator.GenerateAndSaveAsync(progress);
                _lastGenerationDiagnostics = result.DiagnosticLog;
                Timetable.RefreshTimetable();
                ClassViewModel.getInstance().LoadClasses();

                var success = result.Entries.Count > 0 && result.FailedClasses.Count == 0;
                var heading = success ? "Timetable generated successfully" : "No complete timetable generated";
                var failureReason = success ? "" : result.DiagnosticLog.Split(separator, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "No diagnostic information was returned.";
                var elapsed = _generationWatch.Elapsed.ToString(@"hh\:mm\:ss");

                var summary = success
                    ? $"{heading}\n\nTeaching groups: {result.Classes.Count}\nScheduled periods: {result.Entries.Count}\nElapsed: {elapsed}\n\nUse 'Generation Details' to view the full validation report."
                    : $"{heading}\n\nReason: {failureReason}\nElapsed: {elapsed}\n\nUse 'Generation Details' for the full diagnostics."; MessageBox.Show(summary, "Timetable Generation", MessageBoxButton.OK,
                    success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _lastGenerationDiagnostics = ex.ToString();
                MessageBox.Show($"Generation failed: {ex.Message}\n\nSee Generation Details for the full error.", "Timetable Generation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _generationTimer.Stop();
                _generationWatch.Stop();
                IsGenerating = false;
                GenerationStatus = "";
                GenerationPhase = "Ready";
                GenerationDetail = "";
            }
        }

        private void ShowGenerationDiagnostics()
        {
            var window = new Window
            {
                Title = "Timetable Generation Details",
                Width = 850, Height = 650,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                Content = new System.Windows.Controls.TextBox
                {
                    Text = string.IsNullOrWhiteSpace(_lastGenerationDiagnostics) ? "No generation report available yet." : _lastGenerationDiagnostics,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    Margin = new Thickness(12)
                }
            };
            window.ShowDialog();
        }

        public List<Class> GenerateYearGroup(int yearGroupId, List<Student> students, Dictionary<int, Subject> subjects, Dictionary<int, Teacher> teachers, Dictionary<int, Room> rooms)
        {
            var result = new List<Class>();
            int classCounter = 1;

            // students in this year group
            var studentsInYear = students.Where(s => s.CurrentYearGroup == yearGroupId).ToList();

            // all subjects taken by this year group
            var subjectIds = students.SelectMany(s => s.Subjects).Select(s => s.SubjectId).Distinct();

            foreach (var subjectId in subjectIds)
            {
                // students taking this subject
                var subjectStudents = studentsInYear.Where(s => s.Subjects.Any(sub => sub.SubjectId == subjectId)).ToList();

                if (subjectStudents.Count == 0)
                    continue;

                // split into balanced groups
                var shuffled = subjectStudents.OrderBy(_ => Guid.NewGuid()).ToList();
                int groupCount = (int)Math.Ceiling(shuffled.Count / (double)MAX_CLASS_SIZE);
                var groups = Enumerable.Range(0, groupCount).Select(_ => new List<Student>()).ToList();
                for (int i = 0; i < shuffled.Count; i++)
                    groups[i % groupCount].Add(shuffled[i]);

                // pick teacher who teaches subject
                var teacher = teachers.Values.FirstOrDefault(t => t.Subjects.Any(s => s.SubjectId == subjectId));
                if (teacher == null)
                {
                    // handle no teacher case (log, skip, fail generation, etc.)
                    continue;
                }
                var teacherId = teacher.TeacherId;

                // pick any room (later constrained by timetable)
                var roomId = rooms.Values.First().RoomId;

                foreach (var group in groups)
                {
                    var cls = new Class
                    {
                        YearGroupId = yearGroupId,
                        SubjectId = subjectId,
                        TeacherId = teacherId,
                        RoomId = roomId,
                        Name = $"YG{yearGroupId}-{subjects[subjectId].Name}-G{classCounter++}"
                    };

                    // ONLY students in this group
                    foreach (var student in group)
                        cls.Students.Add(student);

                    result.Add(cls);
                }
            }

            foreach (var c in result)
            {
                Console.WriteLine($"{c.Name} → {c.Students.Count} students");
            }

            return result;
        }

        public async Task SaveAsync(List<Class> generated)
        {
            var studentsById = await _context.Students.ToDictionaryAsync(s => s.StudentId);

            foreach (var g in generated)
            {
                var cls = new Class
                {
                    Name = g.Name,
                    YearGroupId = g.YearGroupId,
                    SubjectId = g.SubjectId,
                    TeacherId = g.TeacherId,
                    RoomId = g.RoomId
                };

                foreach (var sid in g.StudentIds)
                    cls.Students.Add(studentsById[sid]);

                _context.Classes.Add(cls);
            }

            await _context.SaveChangesAsync();
        }

    }
}
