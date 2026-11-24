using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollySchoolTimetabler.Models
{
    public class Class : ModelBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ClassId { get; set; }

        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public int RoomId { get; set; }
        public int YearGroupId { get; set; }

        public string? Name { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }

        public Subject Subject { get; set; } = null!;
        public Teacher Teacher { get; set; } = null!;
        public Room Room { get; set; } = null!;
        public YearGroup YearGroup { get; set; } = null!;

        public ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<int> StudentIds { get; set; } = new List<int>();
        public List<(DayOfWeek d, int p)> Slots = new List<(DayOfWeek d, int p)>();

        public override string ToString()
        {
            return $"{DayOfWeek} P{Period} - {Name ?? "No Name"} {Subject?.Name ?? "Unassigned"} ({YearGroup?.Name ?? "No Year"})";
        }
    }
}
