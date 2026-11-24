namespace JollySchoolTimetabler.Models
{
    public class TimetableEntry
    {
        public int TimetableEntryId { get; set; }

        public int StudentId { get; set; }
        public int? ClassId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public int RoomId { get; set; }
        public int YearGroupId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }

        public override string ToString() => $"{YearGroupId} {DayOfWeek} P{Period} TClass={ClassId} Sub={SubjectId} Teacher={TeacherId} Room={RoomId}";
    }
}
