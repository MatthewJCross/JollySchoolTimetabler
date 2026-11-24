using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Events;
using JollySchoolTimetabler.Models;
using JollySchoolTimetabler.Utils;

namespace JollySchoolTimetabler.ViewModels
{
    public class StudentViewModel : ViewModelBase
    {
        private readonly SchoolContext _context;

        public int NextStudentId => Students.Any() ? Students.Max(t => t.StudentId) + 1 : 1;

        public ObservableCollection<Student> Students { get; } = new ObservableCollection<Student>();
        public ObservableCollection<Subject> Subjects { get; set; }

        private Student _selectedStudent;
        public Student SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (_selectedStudent != value)
                {
                    if (_selectedStudent != null)
                        _selectedStudent.PropertyChanged -= SelectedStudentChanged;

                    _selectedStudent = value;
                    OnPropertyChanged();

                    if (_selectedStudent != null)
                        _selectedStudent.PropertyChanged += SelectedStudentChanged;

                    LoadStudentSubjects();

                    if (SelectedStudent != null) TimetableViewModel.getInstance().SetSelectedEntity(SelectedStudent);
                }
            }
        }

        private int _selectedSubjectCount;
        public int SelectedSubjectCount
        {
            get => _selectedSubjectCount;
            set
            {
                _selectedSubjectCount = value;
                OnPropertyChanged(nameof(SelectedSubjectCount));
            }
        }

        private ObservableCollection<Subject> _selectedSubjects;
        public ObservableCollection<Subject> SelectedSubjects
        {
            get => _selectedSubjects;
            set
            {
                _selectedSubjects = value;
                OnPropertyChanged();
                SelectedSubjectCount = _selectedSubjects?.Count ?? 0;
            }
        }

        private string _newStudentFirstName;
        public string NewStudentFirstName
        {
            get => _newStudentFirstName;
            set
            {
                _newStudentFirstName = value;
                OnPropertyChanged();
            }
        }

        private string _newStudentLastName;
        public string NewStudentLastName
        {
            get => _newStudentLastName;
            set
            {
                _newStudentLastName = value;
                OnPropertyChanged();
            }
        }

        private string _newStudentEmail;
        public string NewStudentEmail
        {
            get => _newStudentEmail;
            set
            {
                _newStudentEmail = value;
                OnPropertyChanged();
            }
        }

        private int _newYearGroupId;
        public int NewYearGroupId
        {
            get => _newYearGroupId;
            set { _newYearGroupId = value; OnPropertyChanged(); }
        }

        public string Name => $"{NewStudentFirstName} {NewStudentLastName}";

        public RelayCommand NewStudentCommand { get; }
        public RelayCommand DeleteStudentCommand { get; }
        public RelayCommand UpdateStudentCommand { get; }
        public RelayCommand SaveSubjectsCommand { get; }

        public StudentViewModel(SchoolContext context)
        {
            _context = context;
            NewStudentCommand = new RelayCommand(NewStudent);
            DeleteStudentCommand = new RelayCommand(DeleteStudent, CanDeleteStudent);
            UpdateStudentCommand = new RelayCommand(UpdateStudent);
            SaveSubjectsCommand = new RelayCommand(SaveSubjects, CanSaveSubjects);

            LoadStudents();
            Subjects = new ObservableCollection<Subject>(_context.Subjects.ToList());
            SelectedSubjects = new ObservableCollection<Subject>();

            AppEvents.DatabaseReset += OnDatabaseReset;
            AppEvents.DataChanged += RefreshSubjectOptions;
            SelectedSubjects.CollectionChanged += (s, e) =>
            {
                SelectedSubjectCount = SelectedSubjects.Count;
            };
        }

        ~StudentViewModel()
        {
            AppEvents.DatabaseReset -= OnDatabaseReset;
        }

        private void OnDatabaseReset()
        {
            LoadStudents();
            Subjects = new ObservableCollection<Subject>(_context.Subjects.ToList());
            SelectedSubjects = new ObservableCollection<Subject>();
        }

        private void RefreshSubjectOptions()
        {
            var selectedIds = SelectedSubjects.Select(s => s.SubjectId).ToHashSet();
            var available = _context.Subjects.AsNoTracking().ToList();
            Subjects = new ObservableCollection<Subject>(available);
            OnPropertyChanged(nameof(Subjects));
            // Keep the selection when another tab updates a subject.
            if (SelectedStudent != null && selectedIds.Count > 0)
            {
                SelectedSubjects.Clear();
                foreach (var subject in available.Where(s => selectedIds.Contains(s.SubjectId)))
                    SelectedSubjects.Add(subject);
            }
        }

        private async void LoadStudents()
        {
            var students = await _context.Students
                .Include(s => s.Subjects)
                .AsNoTracking()
                .ToListAsync();

            Students.Clear();
            foreach (var t in students)
                Students.Add(t);
        }

        private async void NewStudent(object parameter)
        {
            var newStudent = new Student
            {
                StudentId = NextStudentId,
                FirstName = string.Empty,
                LastName = string.Empty,
                CurrentYearGroup = 7,
                Email = string.Empty,
                Gender = string.Empty,
            };

            Students.Add(newStudent);
            SelectedStudent = newStudent;

            _context.Students.Add(newStudent);
            _context.SaveChanges();
                AppEvents.RaiseDataChanged();

            SelectedStudent = newStudent;
        }

        private void UpdateStudent(object parameter)
        {
            if (SelectedStudent == null) return;
            SaveStudentScalars(SelectedStudent);
        }

        private void SelectedStudentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Student entity)
                SaveStudentScalars(entity);
        }

        private void SaveStudentScalars(Student entity)
        {
            try
            {
                var tracked = _context.Students.Local.FirstOrDefault(x => x.StudentId == entity.StudentId)
                    ?? _context.Students.Find(entity.StudentId);
                if (tracked == null) return;
                if (!ReferenceEquals(tracked, entity))
                {
                    // Update scalar fields only: do not attach detached Subject graphs.
                    _context.Entry(tracked).CurrentValues.SetValues(entity);
                }
                _context.SaveChanges();
                AppEvents.RaiseDataChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to save student: {ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteStudent(object parameter) => SelectedStudent != null;

        private async void DeleteStudent(object parameter)
        {
            if (SelectedStudent == null) return;

            var result = MessageBox.Show(
                $"Delete {SelectedStudent.FullDetails}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _context.Students.Remove(SelectedStudent);
                await _context.SaveChangesAsync();
                AppEvents.RaiseDataChanged();
                Students.Remove(SelectedStudent);
                SelectedStudent = null;
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Error deleting student: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async void LoadStudentSubjects()
        {
            SelectedSubjects.Clear();

            if (SelectedStudent == null)
                return;

            var student = _context.Students
                .Include(s => s.Subjects)
                .FirstOrDefault(s => s.StudentId == SelectedStudent.StudentId);

            if (student != null)
            {
                foreach (var subj in student.Subjects)
                    SelectedSubjects.Add(Subjects.FirstOrDefault(x => x.SubjectId == subj.SubjectId) ?? subj);
            }

            SelectedSubjectCount = SelectedSubjects.Count;
        }

        private bool CanSaveSubjects(object parameter) => SelectedStudent != null;

        private async void SaveSubjects(object parameter)
        {
            if (SelectedStudent == null) return;
            var selectedIds = SelectedSubjects.Select(s => s.SubjectId).ToHashSet();
            try
            {
                // Load all relationship entities from the SAME tracked context.
                // UI subject options are AsNoTracking and must never be attached directly.
                var student = await _context.Students
                    .Include(x => x.Subjects)
                    .SingleAsync(x => x.StudentId == SelectedStudent.StudentId);
                var trackedSubjects = await _context.Subjects
                    .Where(s => selectedIds.Contains(s.SubjectId))
                    .ToListAsync();
                if (trackedSubjects.Count != selectedIds.Count)
                    throw new InvalidOperationException("One or more selected subjects no longer exist.");
                foreach (var subject in student.Subjects.Where(s => !selectedIds.Contains(s.SubjectId)).ToList())
                    student.Subjects.Remove(subject);
                var existingIds = student.Subjects.Select(s => s.SubjectId).ToHashSet();
                foreach (var subject in trackedSubjects.Where(s => !existingIds.Contains(s.SubjectId)))
                    student.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                AppEvents.RaiseDataChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to save student subjects: {ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
