namespace JollySchoolTimetabler.Models
{
    public class Student : ModelBase
    {
        public int StudentId { get; set; }

        public string FirstName { get; set; } 

        public string LastName { get; set; } 

        public string Gender { get; set; } 

        public string Email { get; set; } 

        public int CurrentYearGroup { get; set; }

        public int? ClassId { get; set; }

        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        // A student attends multiple teaching groups each week.
        public ICollection<Class> Classes { get; set; } = new List<Class>();

        public string FullDetails
        {
            get => $"{ StudentId} {FirstName} {LastName} {Email} {CurrentYearGroup} {ClassId}";
        }
    }
}
