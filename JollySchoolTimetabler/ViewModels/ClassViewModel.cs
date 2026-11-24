using JollySchoolTimetabler.Events;
using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Models;
using JollySchoolTimetabler.Utils;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace JollySchoolTimetabler.ViewModels
{
    public class ClassViewModel : ViewModelBase
    {
        static ClassViewModel _instance = null;
        private readonly SchoolContext _context;

        public ObservableCollection<Class> Classes { get; set; } = new ObservableCollection<Class>();
        public ObservableCollection<Teacher> Teachers { get; set; } = new ObservableCollection<Teacher>();
        public ObservableCollection<Subject> Subjects { get; set; } = new ObservableCollection<Subject>();
        public ObservableCollection<Room> Rooms { get; set; } = new ObservableCollection<Room>();
        public ObservableCollection<YearGroup> YearGroups { get; set; } = new ObservableCollection<YearGroup>();

        public int NextClassId => Classes.Any() ? Classes.Max(t => t.ClassId) + 1 : 1;

        private Class _selectedClass;
        public Class SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (_selectedClass != value)
                {
                    if (_selectedClass != null)
                        _selectedClass.PropertyChanged -= SelectedClassChanged;

                    _selectedClass = value;
                    OnPropertyChanged();

                    if (_selectedClass != null)
                        _selectedClass.PropertyChanged += SelectedClassChanged;

                    TimetableViewModel.getInstance().SetSelectedEntity(SelectedClass);

                }
            }
        }

        public RelayCommand NewClassCommand { get; }
        public RelayCommand DeleteClassCommand { get; }
        public RelayCommand UpdateClassCommand { get; }

        static public ClassViewModel getInstance()
        {
            if (_instance == null)
            {
                _instance = new ClassViewModel(new SchoolContext());
            }

            return _instance;
        }

        public ClassViewModel(SchoolContext context)
        {
            _context = context;
            NewClassCommand = new RelayCommand(NewClass);
            DeleteClassCommand = new RelayCommand(DeleteClass);
            UpdateClassCommand = new RelayCommand(UpdateClass);
            LoadData();
            LoadClasses();
            _instance = this;
        }

        private async void LoadData()
        {
            Teachers = new ObservableCollection<Teacher>(_context.Teachers.ToList());
            Subjects = new ObservableCollection<Subject>(_context.Subjects.ToList());
            Rooms = new ObservableCollection<Room>(_context.Rooms.ToList());
            YearGroups = new ObservableCollection<YearGroup>(_context.YearGroups.ToList());
        }

        public async void LoadClasses()
        {
            Classes = new ObservableCollection<Class>(_context.Classes.ToList());
        }

        private async void NewClass()
        {
            var newClass = new Class
            {
                ClassId = NextClassId,
                TeacherId = 1,
                SubjectId = 1,
                YearGroupId = 7,
                RoomId = 1
            };

            Classes.Add(newClass);
            SelectedClass = newClass;

            _context.Classes.Add(newClass);
            _context.SaveChanges();
                AppEvents.RaiseDataChanged();

            SelectedClass = newClass;
        }

        private void UpdateClass()
        {
            if (SelectedClass != null)
            {
                _context.Classes.Update(SelectedClass);
                _context.SaveChanges();
                AppEvents.RaiseDataChanged();
            }
        }

        private void SelectedClassChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Class _class)
            {
                _context.Classes.Update(_class);
                _context.SaveChanges();
                AppEvents.RaiseDataChanged();
            }
        }

        private async void DeleteClass()
        {
            if (SelectedClass == null) return;

            var result = MessageBox.Show(
                $"Delete {SelectedClass.ToString()}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _context.Classes.Remove(SelectedClass);
                await _context.SaveChangesAsync();
                AppEvents.RaiseDataChanged();
                Classes.Remove(SelectedClass);
                SelectedClass = null;
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Error deleting teacher: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
