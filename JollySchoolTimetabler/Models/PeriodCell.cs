namespace JollySchoolTimetabler.Models
{
    public class PeriodCell
    {
        public DayOfWeek Day { get; set; }
        public int Period { get; set; }
        public string DisplayText { get; set; } = string.Empty;

        // optional helpers
        public string PeriodLabel => $"Period {Period}";
        public string DayLabel => Day.ToString();
    }
}
