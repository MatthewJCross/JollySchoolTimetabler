namespace JollySchoolTimetabler.Models
{
    public class YearGroup : ModelBase
    {
        private int _yearGroupId;
        public int YearGroupId
        {
            get => _yearGroupId;
            set { _yearGroupId = value; OnPropertyChanged(); }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();

        public string FullDetails
        {
            get => $"{YearGroupId} {Name}";
        }
    }
}
