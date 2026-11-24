using JollySchoolTimetabler.Events;
using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Models;
using JollySchoolTimetabler.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JollySchoolTimetabler.ViewModels
{
    public class SubjectViewModel : ViewModelBase
    {
        private readonly SchoolContext _context;
        public ObservableCollection<Subject> Subjects { get; } = new();
        public ObservableCollection<Teacher> SubjectTeachers { get; } = new();
        public int SubjectTeacherCount => SubjectTeachers.Count;
        public int NextSubjectId => Subjects.Any() ? Subjects.Max(t => t.SubjectId) + 1 : 1;

        private Subject _selectedSubject;
        public Subject SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (_selectedSubject != value)
                {
                    if (_selectedSubject != null)
                        _selectedSubject.PropertyChanged -= SelectedSubjectChanged;

                    _selectedSubject = value;
                    OnPropertyChanged();

                    if (_selectedSubject != null)
                        _selectedSubject.PropertyChanged += SelectedSubjectChanged;

                    RefreshSubjectTeachersFromDatabase();
                }
            }
        }

        private string _newSubjectName = "";
        public string NewSubjectName
        {
            get => _newSubjectName;
            set
            {
                _newSubjectName = value;
                OnPropertyChanged();
            }
        }

        private string _newSubjectCode = "";
        public string NewSubjectCode
        {
            get => _newSubjectCode;
            set
            {
                _newSubjectCode = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewSubjectCommand { get; }
        public RelayCommand DeleteSubjectCommand { get; }
        public RelayCommand UpdateSubjectCommand { get; }

        public SubjectViewModel(SchoolContext context)
        {
            _context = context;
            NewSubjectCommand = new RelayCommand(NewSubject);
            DeleteSubjectCommand = new RelayCommand(DeleteSubject);
            UpdateSubjectCommand = new RelayCommand(UpdateSubject);
            LoadSubjects();
            AppEvents.DataChanged += RefreshSubjectTeachersFromDatabase;
        }

        private async void LoadSubjects()
        {
            var subjects = await _context.Subjects.Include(s => s.Teachers).ToListAsync();
            Subjects.Clear();
            foreach (var s in subjects)
                Subjects.Add(s);
        }

        private void RefreshSubjectTeachersFromDatabase()
        {
            if (SelectedSubject == null) return;
            var teachers = _context.Teachers.AsNoTracking()
                .Where(t => t.Subjects.Any(sub => sub.SubjectId == SelectedSubject.SubjectId))
                .OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToList();
            SubjectTeachers.Clear();
            foreach (var teacher in teachers) SubjectTeachers.Add(teacher);
            OnPropertyChanged(nameof(SubjectTeacherCount));
        }

        private void RefreshSubjectTeachers()
        {
            SubjectTeachers.Clear();
            if (SelectedSubject != null)
            {
                foreach (var teacher in SelectedSubject.Teachers.OrderBy(t => t.LastName).ThenBy(t => t.FirstName))
                    SubjectTeachers.Add(teacher);
            }
            OnPropertyChanged(nameof(SubjectTeacherCount));
        }

        private async void NewSubject()
        {
            var newSubject = new Subject
            {
                SubjectId = NextSubjectId,
                Name = string.Empty,
                Code = string.Empty
            };

            Subjects.Add(newSubject);
            SelectedSubject = newSubject;

            _context.Subjects.Add(newSubject);
            _context.SaveChanges();
                AppEvents.RaiseDataChanged();

            SelectedSubject = newSubject;
        }

        private void UpdateSubject()
        {
            if (SelectedSubject != null)
            {
                _context.Subjects.Update(SelectedSubject);
                _context.SaveChanges();
                AppEvents.RaiseDataChanged();
            }
        }

        private void SelectedSubjectChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Subject subject)
            {
                _context.Subjects.Update(subject);
                _context.SaveChanges();
                AppEvents.RaiseDataChanged();
            }
        }

        private async void DeleteSubject()
        {
            if (SelectedSubject == null) return;

            var result = MessageBox.Show(
                $"Delete {SelectedSubject.FullDetails}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _context.Subjects.Remove(SelectedSubject);
                await _context.SaveChangesAsync();
                AppEvents.RaiseDataChanged();
                Subjects.Remove(SelectedSubject);
                SelectedSubject = null;
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Error deleting subject: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
