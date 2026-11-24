using JollySchoolTimetabler.Data;

namespace JollySchoolTimetabler.Services
{
    public class TimetableService
    {
        private readonly SchoolContext _context;

        public TimetableService(SchoolContext context)
        {
            _context = context;
        }
    }
}
