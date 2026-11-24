using JollySchoolTimetabler.Models;
using JollySchoolTimetabler.Utils;

namespace JollySchoolTimetabler.ViewModels
{
    public class DiagnosticViewModel
    {
        public string DiagnosticLog { get; set; } = "";
        public List<DiagnosticFailedClass> FailedClasses { get; set; } = new();
    }
}
