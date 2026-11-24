using JollySchoolTimetabler.Events;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using JollySchoolTimetabler.Data;
using JollySchoolTimetabler.Models;
using JollySchoolTimetabler.Utils;

namespace JollySchoolTimetabler.ViewModels
{
    public class RoomViewModel : ViewModelBase
    {
        private readonly SchoolContext _context;
        public ObservableCollection<Room> Rooms { get; } = new ObservableCollection<Room>();
        public int NextRoomId => Rooms.Any() ? Rooms.Max(t => t.RoomId) + 1 : 1;

        private Room _selectedRoom;
        public Room SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (_selectedRoom != value)
                {
                    _selectedRoom = value;
                    OnPropertyChanged();

                    if (_selectedRoom != null)
                    {
                        EditRoomName = _selectedRoom.Name ?? "";
                        EditRoomCapacity = _selectedRoom.Capacity.ToString();
                        if (TimetableViewModel.getInstance().SelectedRoom?.RoomId != _selectedRoom.RoomId)
                            TimetableViewModel.getInstance().SetSelectedEntity(_selectedRoom);
                    }
                }
            }
        }

        private string _editRoomName = "";
        public string EditRoomName
        {
            get => _editRoomName;
            set 
            { 
                _editRoomName = value; 
                OnPropertyChanged();
            }
        }

        private string _editRoomCapacity = "30";
        public string EditRoomCapacity
        {
            get => _editRoomCapacity;
            set 
            { 
                _editRoomCapacity = value; 
                OnPropertyChanged();
            }
        }

        private string _newRoomName = "";
        public string NewRoomName
        {
            get => _newRoomName;
            set
            {
                _newRoomName = value;
                OnPropertyChanged();
            }
        }

        private int _newRoomCapacity = 30;
        public int NewRoomCapacity
        {
            get => _newRoomCapacity;
            set
            {
                _newRoomCapacity = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NewRoomCommand { get; }
        public RelayCommand DeleteRoomCommand { get; }
        public RelayCommand UpdateRoomCommand { get; }

        public RoomViewModel(SchoolContext context)
        {
            _context = context;
            NewRoomCommand = new RelayCommand(NewRoom);
            DeleteRoomCommand = new RelayCommand(DeleteRoom);
            UpdateRoomCommand = new RelayCommand(UpdateRoom);
            var timetable = TimetableViewModel.getInstance();
            timetable.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(TimetableViewModel.SelectedRoom) && timetable.SelectedRoom != null)
                    SelectedRoom = Rooms.FirstOrDefault(r => r.RoomId == timetable.SelectedRoom.RoomId);
            };
            AppEvents.DatabaseReset += LoadRooms;
            LoadRooms();
        }

        private async void LoadRooms()
        {
            var rooms = await _context.Rooms.ToListAsync();
            Rooms.Clear();
            foreach (var r in rooms)
                Rooms.Add(r);

            var selectedId = TimetableViewModel.getInstance().SelectedRoom?.RoomId;
            if (selectedId.HasValue)
                SelectedRoom = Rooms.FirstOrDefault(r => r.RoomId == selectedId.Value);
        }

        private async void NewRoom()
        {
            var newRoom = new Room
            {
                RoomId = NextRoomId,
                Name = string.Empty,
                Capacity = 30
            };

            Rooms.Add(newRoom);
            SelectedRoom = newRoom;

            _context.Rooms.Add(newRoom);
            _context.SaveChanges();
            AppEvents.RaiseDataChanged();

            SelectedRoom = newRoom;
        }

        private void UpdateRoom()
        {
            if (SelectedRoom == null) return;
            if (string.IsNullOrWhiteSpace(EditRoomName) ||
                !int.TryParse(EditRoomCapacity, out var capacity) || capacity < 1)
            {
                MessageBox.Show("Enter a room name and a positive whole-number capacity.", "Invalid room");
                return;
            }
            try
            {
                var room = _context.Rooms.Local.FirstOrDefault(r => r.RoomId == SelectedRoom.RoomId)
                    ?? _context.Rooms.Single(r => r.RoomId == SelectedRoom.RoomId);
                room.Name = EditRoomName.Trim();
                room.Capacity = capacity;
                _context.SaveChanges();
                OnPropertyChanged(nameof(SelectedRoom));
                AppEvents.RaiseDataChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to save room: {ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteRoom()
        {
            if (SelectedRoom == null) return;

            var result = MessageBox.Show($"Delete {SelectedRoom.FullDetails}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _context.Rooms.Remove(SelectedRoom);
                await _context.SaveChangesAsync();
                AppEvents.RaiseDataChanged();
                Rooms.Remove(SelectedRoom);
                SelectedRoom = null;
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show($"Error deleting room: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
