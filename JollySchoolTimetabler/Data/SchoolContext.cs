using Microsoft.EntityFrameworkCore;
using JollySchoolTimetabler.Models;
using Microsoft.Extensions.Logging;

namespace JollySchoolTimetabler.Data
{
    public class SchoolContext : DbContext
    {
        public SchoolContext()
        {
        }

        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        // Configure default DB connection (SQLite in this case)
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseSqlite("Data Source=school.db")
                    .EnableSensitiveDataLogging()
                    .LogTo(Console.WriteLine, LogLevel.Information);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Explicit many-to-many: Student.ClassId is a legacy single-class field,
            // not the relationship used by the weekly timetable.
            modelBuilder.Entity<Class>()
                .HasMany(c => c.Students)
                .WithMany(s => s.Classes)
                .UsingEntity<Dictionary<string, object>>(
                    "StudentClassMembership",
                    r => r.HasOne<Student>().WithMany().HasForeignKey("StudentId").OnDelete(DeleteBehavior.Cascade),
                    l => l.HasOne<Class>().WithMany().HasForeignKey("ClassId").OnDelete(DeleteBehavior.Cascade),
                    j => { j.HasKey("ClassId", "StudentId"); j.ToTable("StudentClassMembership"); });
        }

        // DbSets for your entities
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<YearGroup> YearGroups { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<TimetableEntry> TimetableEntries { get; set; }
        public DbSet<Class> Classes { get; set; }
    }
}

