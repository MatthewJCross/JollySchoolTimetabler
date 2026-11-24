using JollySchoolTimetabler.Models;

namespace JollySchoolTimetabler.Utils
{
    public class DiagnosticFailedClass
    {
        public Class Class { get; set; }
        public string Reason { get; set; }

        public override string ToString() => $"{Class?.Name} — {Reason}";
    }

    public class DiagnosticResult
    {
        public List<string> DiagnosticLog { get; set; } = new();
        public List<DiagnosticFailedClass> FailedClasses { get; set; } = new();
    }
}
