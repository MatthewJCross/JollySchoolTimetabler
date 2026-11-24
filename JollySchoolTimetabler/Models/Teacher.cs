
namespace JollySchoolTimetabler.Models
{
    public class Teacher : ModelBase
    {
        private int _teacherId;
        public int TeacherId
        {
            get => _teacherId;
            set
            { 
                _teacherId = value; 
                OnPropertyChanged();
            }
        }

        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set 
            { 
                _firstName = value; 
                OnPropertyChanged(); 
            }
        }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set
            { 
                _lastName = value;
                OnPropertyChanged();
            }
        }

        private string _gender;
        public string Gender
        {
            get => _gender;
            set
            {
                _gender = value; OnPropertyChanged();
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string FullName
        {
            get => $"{FirstName} {LastName}";
        }

        public string FullDetails
        {
            get => $"{FullName} {Email}";
        }

        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public ICollection<YearGroup> YearGroups { get; set; } = new List<YearGroup>();

        public bool CanTeach(Subject subject)
        {
            if (Subjects.Any(s => s.Name == subject.Name))
                //if (Subjects.Contains(subject)) 
                return true;

            return false;
        }
    }
}
