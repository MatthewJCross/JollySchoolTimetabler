namespace JollySchoolTimetabler.Models
{
    public class Subject : ModelBase
    {
        private int _subjectId;
        public int SubjectId
        {
            get => _subjectId;
            set
            {
                _subjectId = value;
                OnPropertyChanged();
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value; OnPropertyChanged();
            }
        }

        private string _code; // e.g., MATH101
        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                OnPropertyChanged();
            }
        }

        public string FullDetails
        {
            get => $"{Name} {Code}";
        }

        public int RequiredPeriods { get; set; } = 1;
        public bool RequiresDoublePeriod { get; set; } = false;

        public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
