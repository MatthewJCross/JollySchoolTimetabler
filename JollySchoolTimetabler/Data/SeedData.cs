using JollySchoolTimetabler.Models;
using Microsoft.EntityFrameworkCore;

namespace JollySchoolTimetabler.Data
{
    public static class SeedData
    {
        private static readonly string[] maleFirstNames =
        {
            "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Charles", "Matthew", "Jack", "Kevin", "Jeffrey", "Kirk", "Barry", "Gregory",
            "Paul", "Stuart", "Jamie", "Andrew", "Roger", "Quentin", "Zack", "Ryan", "Donald", "Jeremy", "Graham", "Ashley", "Lee", "Gerald", "Julian", "Geoffrey", "Harry", "Frank",
            "Oliver", "Henry", "Freddie", "Archie", "Lucas", "Logan", "Finley", "Hugo", "Luca", "Roman", "Stanley", "Liam", "Harley", "Rowan", "Felix", "Blake", "Calum", "Bryn", "Ewan",
            "Dorian", "Jules", "Miles", "Ozzy", "Russel", "Sonny", "Vinnie", "Apollo", "Brady", "Colt", "Jax", "Kane", "Sage", "Tate", "Wyatt", "Ashton", "Dalton", "Orlando", "Kirk",
            "Arnold", "Seamus", "Terry", "Jacob", "Eugene", "Martin", "Alexander", "Tommy", "Ian", "David", "Brian", "Anthony", "Mark", "Steven", "Paul", "Joshua", "Kenneth", "George", 
            "Timothy", "Ronald", "Edward", "Jason", "Gary", "Nicholas", "Eric", "Stephen", "Larry", "Justin", "Scott", "Brandon", "Benjamin", "Adam", "Samuel", "Franklin", "Raymond",
        };
                
        private static readonly string[] femaleFirstNames = 
        {
            "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Julie", "Barbara", "Susan", "Jessica", "Sarah", "Karen", "Tracey", "Katie", "Barbara", "Gemma", "Catherine", "Olivia",
            "Paris", "Vanessa", "Sandra", "Rebecca", "Lisa", "Alice", "Rachel", "Zena", "Kimberley", "Avril", "Ashleigh", "Kerry", "Gillian", "Cathy", "Britney", "Rosey", "Ava", "Madison",
            "Sky", "Freya", "Mabel", "Sophie", "Marie", "Ruby", "Eva", "Charlotte", "Mia", "Erin", "Robyn", "Hannah", "Clara", "Josie", "Claire", "Jodie", "Holly", "Lucy", "Maria", "Brianna",
            "Blaire", "Davina", "Harlow", "Indie", "Kendal", "Lola", "Morgan", "Sasha", "Tatum", "Wanda", "Jordan", "Paris", "Sydney", "Celia", "Daniella", "Eliza", "Evelynn", "Kylie", "Paloma",
            "Mariella", "Keeley", "Ginney", "Haley", "Gracy", "Martina", "Sable", "Louise", "Alexis", "Rowina", "Monika", "Abigail", "Stephanie", "Jade", "Olivia", "Geogia", "Deborah", "Helen",
            "Amanda", "Emily", "Michelle", "Danielle", "Nicola", "Rae", "Angela", "Linda", "Sandra", "Sharon", "Debbie", "Carol", "Diane", "Brenda", "Pamela", "Nicole", "Katherine", "Janet", "Cynthia", 
            "Christine", "Samantha", "Kathleen", "Debra", "Rachael"
        };

        private static readonly string[] LastNames = 
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "England", "Lamb", "Garrod","Lucas", "Watson", "Butler", "French", "Frankum", "Kurtz", "Cunningham",
            "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "King", "Norman", "Wilkinson", "Price", "Wright", "Terry", "Casey", "Robinson", "Tatum", "Ryan",
            "Black", "White", "Green", "Stamp", "Groves", "Harding", "Pierce", "England", "Sayer", "Cross", "Goss", "Prince", "Hancock", "Baker", "Hamilton", "Jackson", "Hughes", "Campbell",
            "Rose", "Miles", "Lane", "Kelly", "Pople", "Shortman", "Hurd", "Piper", "Johnson", "Williams", "Taylor", "Story", "Wall", "Babcock", "Curry", "Sweet", "McManus", "Moran", "Lyons",
            "Douglas", "Doyle", "Fish", "Parker", "Schofield", "Riddick", "Lee", "Thompson", "Martin", "Moment", "Popham", "Gary", "Loius", "Screen", "Rothery", "Kinney", "Donelly", "Kurtz",
            "Wayne", "Waller", "Peacock", "Ham", "Swift", "Berryman", "Fields", "Wadlow", "Shave", "Plumb", "Bullock", "Wade", "Barker", "Toms", "Kneigthly", "Robson", "Hayes", "Burke", "Frost",
            "Duffy", "Farrel", "Healy", "Whealan", "Maguire", "Reid", "Griffin", "Lynch", "Sweeney", "Doherty", "Thompson", "McKenna", "Healey", "Buckley", "Nolan", "Foley", "Carroll", "Dunne",
            "Scott", "Biggs", "Flyne", "Bennets", "Lee", "Mitchell", "Young", "Cookson", "Marshall", "Foster", "Powell", "Rogers", "Simpson", "Gray", "Hunt", "Campbell", "Butler", "Jenkins",
            "Mills", "Briggs", "Pearson", "Walsh", "Elliot", "Howard", "Payne", "Fox", "Lawrence", "Coles", "Bradley", "Atkins", "Dawson", "Armstrong", "Rees", "Hart", "Hunter", "Page", "Woods",
            "Stone", "Porter", "Newton", "Harding", "Bird", "Perry", "Bishop", "Burgess", "Shepherd", "Warren", "Nichols", "Hodgson", "Coleman", "McCarthy", "Chambers", "May", "Hammond", "Nelson",
            "Hawes", "Braithwaite", "Kinney", "Hall", "Hotchkiss", "Philpot", "Frostrop", "DeForrest", "Whittaker", "Baxster", "Norris", "Hamilton", "Simmonds", "Corbett", "Bryant", "Glover",
            "Whittaker", "Lambert", "Coles", "Stark", "Hawkins", "Bennett", "Harrison", "Morrison", "Fleming", "Holland", "Cameron", "Gibson", "Webb", "Murray", "Ford", "Wells", "Knight", "Hawthorne"
        };

        private static readonly string[] RoomNames =
        {
            "Maple", "Oak", "Ash", "Beech", "Birch", "Hawthorn", "Spruce", "Walnut", "Pine", "Cherry", "Sycamore", "Willow", "Yew", "Elder", "Elm", "Alder", "Blackthorne", "Fir", "Hazel", "Poplar",
            "Apple", "Aspen", "Horse Chestnut", "Spruce", "Cedar", "Rose", "Holly", "Wither", "Juniper", "Pear", "Olive", "Walnut", "Cypress", "Magnolia", "Dogwood", "Sequoia", "Redwood", "Larch", 
            "Cork Oak", "Balsa", "Teak" 
        };  

        private static readonly string[] WeekDays = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

        private static readonly int[] Periods = { 1, 2, 3, 4, 5 };

        private static readonly Dictionary<int, (TimeSpan Start, TimeSpan End)> PeriodTimes =
            new Dictionary<int, (TimeSpan, TimeSpan)>
            {
                { 1, (new TimeSpan(9, 0, 0),  new TimeSpan(9, 50, 0)) },
                { 2, (new TimeSpan(9, 55, 0), new TimeSpan(10, 45, 0)) },
                { 3, (new TimeSpan(11, 0, 0), new TimeSpan(11, 50, 0)) },
                { 4, (new TimeSpan(12, 40, 0), new TimeSpan(13, 30, 0)) },
                { 5, (new TimeSpan(13, 35, 0), new TimeSpan(14, 25, 0)) },
            };

        // --- SUBJECTS ---
        private static readonly List<Subject> coreSubjects = new List<Subject>
        {
            new Subject { Name = "English", Code = "ENG", RequiredPeriods = 5 },
            new Subject { Name = "Mathematics", Code = "MATH", RequiredPeriods = 5 },
            new Subject { Name = "Science", Code = "SCI", RequiredPeriods = 5 },
            new Subject { Name = "History", Code = "HIS", RequiredPeriods = 3 },
            new Subject { Name = "Geography", Code = "GEO", RequiredPeriods = 3 },
            new Subject { Name = "Physical Education", Code = "PE", RequiredPeriods = 3 },
            new Subject { Name = "Computing", Code = "COMP", RequiredPeriods = 2}
        };

        private static readonly List<Subject> nonCoreSubjects = new List<Subject>
        {
            new Subject { Name = "Art", Code = "ART", RequiredPeriods = 1 },
            new Subject { Name = "Music", Code = "MUS", RequiredPeriods = 1 },
            new Subject { Name = "Drama", Code = "DRM", RequiredPeriods = 1 },
            new Subject { Name = "Design & Technology", Code = "DT", RequiredPeriods = 1 },
            new Subject { Name = "French", Code = "FRN", RequiredPeriods = 1 },
            new Subject { Name = "Spanish", Code = "SPN", RequiredPeriods = 1 },
            new Subject { Name = "Business Studies", Code = "BUS", RequiredPeriods = 1 },
            new Subject { Name = "Religious Education", Code = "RE", RequiredPeriods = 1 }
        };

        private static readonly Random random = new Random(20261123);

        public static void Initialize(SchoolContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Clear tracker (just in case)
            context.ChangeTracker.Clear();

            SeedYearGroups(context);
            SeedSubjects(context);
            SeedTeachers(context);
            SeedRooms(context);
            SeedStudents(context);
            // Curriculum is stored outside the database. Recreate it alongside the
            // newly seeded database so stale year-specific overrides cannot survive.
            JollySchoolTimetabler.Services.Scheduling.CurriculumSettings.ResetForSeed(context.YearGroups.Select(y => y.YearGroupId).ToArray(), context.Subjects.Select(s => new { s.SubjectId, s.Code }).AsEnumerable().Select(s => (Id: s.SubjectId, Code: (string?)s.Code)).ToArray());
        }

        private static void SeedYearGroups(SchoolContext context)
        {
            if (context.YearGroups.Any())
                return;

            var years = new[]
            {
                new YearGroup { Name = "Year 7", YearGroupId = 7 },
                new YearGroup { Name = "Year 8", YearGroupId = 8 },
                new YearGroup { Name = "Year 9", YearGroupId = 9 },
                new YearGroup { Name = "Year 10", YearGroupId = 10 },
                new YearGroup { Name = "Year 11", YearGroupId = 11 },
            };

            context.YearGroups.AddRange(years);
            context.SaveChanges();
        }

        private static void SeedSubjects(SchoolContext context)
        {
            if (context.Subjects.Any())
                return;

            int id = 1;
            var subjects = coreSubjects.Concat(nonCoreSubjects).Select(subject => new Subject
            {
                Name = subject.Name, SubjectId = id++, Code = subject.Code,
                RequiredPeriods = subject.RequiredPeriods
            }).ToList();

            context.Subjects.AddRange(subjects);
            context.SaveChanges();
        }

        private static void SeedTeachers(SchoolContext context)
        {
            if (context.Teachers.Any())
                return;

            var subjects = context.Subjects.OrderBy(s => s.SubjectId).ToList();
            var teachers = new List<Teacher>();
            var coverage = subjects.ToDictionary(s => s.SubjectId, _ => 0);
            var teacherSubjects = new List<List<Subject>>();

            // Allocate the first qualification by expected weekly demand.
            // Every subject receives coverage, including smaller options.
            for (int i = 0; i < 40; i++)
            {
                var first = subjects.OrderBy(s => (double)coverage[s.SubjectId] / Math.Max(1, s.RequiredPeriods)).ThenBy(s => coverage[s.SubjectId]).ThenBy(_ => random.Next()).First();
                teacherSubjects.Add(new List<Subject> { first });
                coverage[first.SubjectId]++;
            }

            // A second qualification for every teacher, balancing qualified
            // staff against subject demand rather than choosing at random.
            for (int i = 0; i < teacherSubjects.Count; i++)
            {
                var second = subjects.Where(s => !teacherSubjects[i].Any(x => x.SubjectId == s.SubjectId)).OrderBy(s => (double)coverage[s.SubjectId] / Math.Max(1, s.RequiredPeriods)).ThenBy(s => coverage[s.SubjectId]).ThenBy(_ => random.Next()).First();
                teacherSubjects[i].Add(second);
                coverage[second.SubjectId]++;
            }

            // Add a third qualification only when a subject has unusually
            // low coverage relative to demand. Limit this to eight teachers.
            for (int extra = 0; extra < 8; extra++)
            {
                var mostNeeded = subjects.OrderBy(s => (double)coverage[s.SubjectId] / Math.Max(1, s.RequiredPeriods)).First();
                var candidate = Enumerable.Range(0, teacherSubjects.Count).Where(i => teacherSubjects[i].Count == 2 && teacherSubjects[i].All(s => s.SubjectId != mostNeeded.SubjectId)).OrderBy(i => teacherSubjects[i].Sum(s => s.RequiredPeriods)).FirstOrDefault(-1);
                if (candidate < 0) 
                    break;

                // Two qualifications are preferred; a third is only needed
                // for core subjects whose coverage is below six teachers.
                if (mostNeeded.RequiredPeriods < 4 || coverage[mostNeeded.SubjectId] >= 6)
                    break;

                teacherSubjects[candidate].Add(mostNeeded);
                coverage[mostNeeded.SubjectId]++;
            }

            for (int i = 0; i < teacherSubjects.Count; i++)
            {
                bool isMale = i < 15;
                string first = isMale ? maleFirstNames[random.Next(maleFirstNames.Length)] : femaleFirstNames[random.Next(femaleFirstNames.Length)];
                string last = LastNames[random.Next(LastNames.Length)];
                teachers.Add(new Teacher
                {
                    TeacherId = i + 1, FirstName = first, LastName = last,
                    Gender = isMale ? "Male" : "Female",
                    Email = $"{first}.{last}@school.com",
                    Subjects = teacherSubjects[i]
                });
            }

            context.Teachers.AddRange(teachers);
            context.SaveChanges();
        }

        private static void SeedRooms(SchoolContext context)
        {
            if (context.Rooms.Any())
                return;

            var rooms = new List<Room>();
            int id = 1;

            foreach (var room in RoomNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var newRoom = new Room
                {
                    RoomId = id++,
                    Name = room,
                    Capacity = random.Next(28, 32)
                };

                rooms.Add(newRoom);
            }

            context.Rooms.AddRange(rooms);
            context.SaveChanges();
        }

        private static void SeedStudents(SchoolContext context)
        {
            if (context.Students.Any())
                return;

            var yearGroups = context.YearGroups.ToList();
            var allSubjects = context.Subjects.ToList();
            var coreSubjects = allSubjects.Where(s => new[] { "ENG", "MATH", "SCI", "HIS", "GEO", "PE", "COMP" }.Contains(s.Code)).ToList();

            var nonCoreSubjects = allSubjects.Except(coreSubjects).ToList();
            // Two balanced option pathways. Each student takes four options, one
            // from each shared option block. This makes a fully occupied week
            // structurally schedulable instead of generating arbitrary clashes.
            var optionPathA = nonCoreSubjects.OrderBy(s => s.SubjectId).Take(4).ToArray();
            var optionPathB = nonCoreSubjects.OrderBy(s => s.SubjectId).Skip(4).Take(4).ToArray();
            if (optionPathA.Length != 4 || optionPathB.Length != 4)
                throw new InvalidOperationException("The seed requires eight optional subjects.");

            var students = new List<Student>();
            int id = 1;

            foreach (var yearGroup in yearGroups)
            {

                // 115–130 students per group
                int count = random.Next(115, 131);

                for (int i = 0; i < count; i++)
                {
                    bool isMale = i < 65;
                    string first = isMale ? maleFirstNames[random.Next(maleFirstNames.Length)] : femaleFirstNames[random.Next(femaleFirstNames.Length)];

                    string last = LastNames[random.Next(LastNames.Length)];

                    var student = new Student
                    {
                        StudentId = id++,
                        FirstName = first,
                        LastName = last,
                        Gender = isMale ? "Male" : "Female",
                        Email = $"{first}.{last}@school.com",
                        CurrentYearGroup = yearGroup.YearGroupId,
                        Subjects = new List<Subject>()
                    };

                    // Alternate pathways to balance uptake within every year group.
                    // The generator places the matching option pairs in parallel.
                    var chosenNonCore = (i % 2 == 0 ? optionPathA : optionPathB);

                    foreach (var subj in coreSubjects.Concat(chosenNonCore))
                        student.Subjects.Add(subj);

                    students.Add(student);
                }
            }

            context.Students.AddRange(students);
            context.SaveChanges();
        }
    }
}
