using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Events;
using JollySchoolTimetabler.Models;
using JollySchoolTimetabler.Utils;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace JollySchoolTimetabler.ViewModels
{
    public class TeacherViewModel : ViewModelBase
    {
        private readonly SchoolContext _context;

        public int NextTeacherId => Teachers.Any() ? Teachers.Max(t => t.TeacherId) + 1 : 1;

        public ObservableCollection<Teacher> Teachers { get; } = new ObservableCollection<Teacher>();
        public ObservableCollection<Subject> Subjects { get; set; }

        private Teacher _selectedTeacher;
        public Teacher SelectedTeacher
        {
            get => _selectedTeacher;
            set
            {
                if (_selectedTeacher != value)
                {
                    if (_selectedTeacher != null)
                        _selectedTeacher.PropertyChanged -= SelectedTeacherChanged;

                    _selectedTeacher = value;
                    OnPropertyChanged();

                    if (_selectedTeacher != null)
                        _selectedTeacher.PropertyChanged += SelectedTeacherChanged;

                    LoadTeacherSubjects();

                    TimetableViewModel.getInstance().SetSelectedEntity(SelectedTeacher);
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

        private string _newTeacherFirstName;
        public string NewTeacherFirstName
        {
            get => _newTeacherFirstName;
            set
            {
                _newTeacherFirstName = value;
                OnPropertyChanged();
            }
        }

        private string _newTeacherLastName;
        public string NewTeacherLastName
        {
            get => _newTeacherLastName;
            set
            {
                _newTeacherLastName = value;
                OnPropertyChanged();
            }
        }

        private string _newTeacherEmail;
        public string NewTeacherEmail
        {
            get => _newTeacherEmail;
            set
            {
                _newTeacherEmail = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewTeacherCommand { get; }
        public RelayCommand DeleteTeacherCommand { get; }
        public RelayCommand UpdateTeacherCommand { get; }
        public RelayCommand SaveSubjectsCommand { get; }

        public TeacherViewModel(SchoolContext context)
        {
            _context = context;
            NewTeacherCommand = new RelayCommand(NewTeacher);
            DeleteTeacherCommand = new RelayCommand(DeleteTeacher, CanDeleteTeacher);
            UpdateTeacherCommand = new RelayCommand(UpdateTeacher);
            SaveSubjectsCommand = new RelayCommand(SaveSubjects, CanSaveSubjects);

            LoadTeachers();
            Subjects = new ObservableCollection<Subject>(_context.Subjects.ToList());
            SelectedSubjects = new ObservableCollection<Subject>();

            AppEvents.DatabaseReset += OnDatabaseReset;
            AppEvents.DataChanged += RefreshSubjectOptions;
            SelectedSubjects.CollectionChanged += (s, e) =>
            {
                SelectedSubjectCount = SelectedSubjects.Count;
            };
        }

        ~TeacherViewModel()
        {
            AppEvents.DatabaseReset -= OnDatabaseReset;
        }

        private void OnDatabaseReset()
        {
            LoadTeachers();
            Subjects = new ObservableCollection<Subject>(_context.Subjects.ToList());
            SelectedSubjects = new ObservableCollection<Subject>();
        }

        private void RefreshSubjectOptions()
        {
            Subjects = new ObservableCollection<Subject>(_context.Subjects.AsNoTracking().ToList());
            OnPropertyChanged(nameof(Subjects));
        }

        private async void LoadTeachers()
        {
            var teachers = await _context.Teachers
                .Include(s => s.Subjects)
                .AsNoTracking()
                .ToListAsync();

            Teachers.Clear();
            foreach (var t in teachers)
                Teachers.Add(t);
        }

        private async void NewTeacher()
        {
            var newTeacher = new Teacher
            {
                TeacherId = NextTeacherId,
                FirstName = string.Empty,
                LastName = string.Empty,
                Email = string.Empty,
                Gender = string.Empty,
            };

            Teachers.Add(newTeacher);
            SelectedTeacher = newTeacher;

            _context.Teachers.Add(newTeacher);
            _context.SaveChanges();
                AppEvents.RaiseDataChanged(); 

            SelectedTeacher = newTeacher;
        }

        private void UpdateTeacher()
        {
            if (SelectedTeacher == null) return;
            SaveTeacherScalars(SelectedTeacher);
        }

        private void SelectedTeacherChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Teacher entity)
                SaveTeacherScalars(entity);
        }

        private void SaveTeacherScalars(Teacher entity)
        {
            try
            {
                var tracked = _context.Teachers.Local.FirstOrDefault(x => x.TeacherId == entity.TeacherId)
                    ?? _context.Teachers.Find(entity.TeacherId);
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
                MessageBox.Show($"Unable to save teacher: {ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteTeacher(object parameter) => SelectedTeacher != null;

        private async void DeleteTeacher(object parameter)
        {
            if (SelectedTeacher == null) return;

            var result = MessageBox.Show(
                $"Delete {SelectedTeacher.FullDetails}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _context.Teachers.Remove(SelectedTeacher);
                await _context.SaveChangesAsync();
                AppEvents.RaiseDataChanged();
                Teachers.Remove(SelectedTeacher);
                SelectedTeacher = null;
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Error deleting teacher: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async void LoadTeacherSubjects()
        {
            SelectedSubjects.Clear();

            if (SelectedTeacher == null)
                return;

            var teacher = _context.Teachers
                .Include(s => s.Subjects)
                .FirstOrDefault(s => s.TeacherId == SelectedTeacher.TeacherId);

            if (teacher != null)
            {
                foreach (var subj in teacher.Subjects)
                    SelectedSubjects.Add(Subjects.FirstOrDefault(x => x.SubjectId == subj.SubjectId) ?? subj);
            }

            SelectedSubjectCount = SelectedSubjects.Count;
        }

        private bool CanSaveSubjects(object parameter) => SelectedTeacher != null;

        private async void SaveSubjects(object parameter)
        {
            if (SelectedTeacher == null) return;
            var selectedIds = SelectedSubjects.Select(s => s.SubjectId).ToHashSet();
            try
            {
                // Load all relationship entities from the SAME tracked context.
                // UI subject options are AsNoTracking and must never be attached directly.
                var teacher = await _context.Teachers
                    .Include(x => x.Subjects)
                    .SingleAsync(x => x.TeacherId == SelectedTeacher.TeacherId);
                var trackedSubjects = await _context.Subjects
                    .Where(s => selectedIds.Contains(s.SubjectId))
                    .ToListAsync();
                if (trackedSubjects.Count != selectedIds.Count)
                    throw new InvalidOperationException("One or more selected subjects no longer exist.");
                foreach (var subject in teacher.Subjects.Where(s => !selectedIds.Contains(s.SubjectId)).ToList())
                    teacher.Subjects.Remove(subject);
                var existingIds = teacher.Subjects.Select(s => s.SubjectId).ToHashSet();
                foreach (var subject in trackedSubjects.Where(s => !existingIds.Contains(s.SubjectId)))
                    teacher.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                AppEvents.RaiseDataChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to save teacher subjects: {ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
