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
    public class YearGroupViewModel : ViewModelBase
    {
        private readonly SchoolContext _context;
        public ObservableCollection<YearGroup> YearGroups { get; } = new();

        public int NextYearGroupId => YearGroups.Any() ? YearGroups.Max(t => t.YearGroupId) + 1 : 1;

        private YearGroup _selectedYearGroup;
        public YearGroup SelectedYearGroup
        {
            get => _selectedYearGroup;
            set
            {
                if (_selectedYearGroup != value)
                {
                    if (_selectedYearGroup != null)
                        _selectedYearGroup.PropertyChanged -= SelectedYearGroupChanged;

                    _selectedYearGroup = value;
                    OnPropertyChanged();

                    if (_selectedYearGroup != null)
                        _selectedYearGroup.PropertyChanged += SelectedYearGroupChanged;
                }
            }
        }

        private string _newYearGroupName = "";
        public string NewYearGroupName
        {
            get => _newYearGroupName;
            set { _newYearGroupName = value; OnPropertyChanged(); }
        }

        public RelayCommand NewYearGroupCommand { get; }
        public RelayCommand DeleteYearGroupCommand { get; }
        public RelayCommand UpdateYearGroupCommand { get; }

        public YearGroupViewModel(SchoolContext context)
        {
            _context = context;
            NewYearGroupCommand = new RelayCommand(NewYearGroup);
            DeleteYearGroupCommand = new RelayCommand(DeleteYearGroup);
            UpdateYearGroupCommand = new RelayCommand(UpdateYearGroup);
            LoadYearGroups();
        }

        private void SelectedYearGroupChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is YearGroup yearGroup)
            {
                _context.YearGroups.Update(yearGroup);
                _context.SaveChanges();
                AppEvents.RaiseDataChanged();
            }
        }

        private async void LoadYearGroups()
        {
            var groups = await _context.YearGroups.ToListAsync();
            YearGroups.Clear();
            foreach (var g in groups) 
                YearGroups.Add(g);
        }

        private async void NewYearGroup()
        {
            var newYearGroup = new YearGroup
            {
                YearGroupId = NextYearGroupId,
                Name = string.Empty
            };

            YearGroups.Add(newYearGroup);
            SelectedYearGroup = newYearGroup;

            _context.YearGroups.Add(newYearGroup);
            _context.SaveChanges();
                AppEvents.RaiseDataChanged();

            SelectedYearGroup = newYearGroup;
        }

        private void UpdateYearGroup()
        {
            if (SelectedYearGroup != null)
            {
                _context.YearGroups.Update(SelectedYearGroup);
                _context.SaveChanges();
                AppEvents.RaiseDataChanged();
            }
        }

        private async void DeleteYearGroup()
        {
            if (SelectedYearGroup == null) 
                return;

            var result = MessageBox.Show(
                $"Delete {SelectedYearGroup.FullDetails}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) 
                return;

            try
            {
                _context.YearGroups.Remove(SelectedYearGroup);
                await _context.SaveChangesAsync();
                AppEvents.RaiseDataChanged();
                YearGroups.Remove(SelectedYearGroup);
                SelectedYearGroup = null;
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Error deleting student: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
