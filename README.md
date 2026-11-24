# JollySchoolTimetabler

A Windows desktop school timetabling application built with **WPF**, **.NET 9**, **Entity Framework Core** and **Google OR-Tools CP-SAT**. It manages school resources and attempts to generate conflict-free weekly timetables for students, teachers and rooms.

> **Development status:** Work in progress. Full timetable generation is not yet guaranteed for every dataset. Infeasible runs provide diagnostic partial solutions rather than saving incomplete timetables.

## Features

- Manage **students, teachers, subjects, rooms, classes and year groups**.
- Assign subject choices to students and teaching qualifications to teachers.
- View weekly timetables by **student, teacher or room**.
- Edit room details, including name and capacity.
- Configure subject requirements separately for each year group using the **Curriculum Configurator**.
- Generate teaching groups according to student demand and room/class-size limits.
- Solve timetable assignments with **OR-Tools CP-SAT**, checking student, teacher and room conflicts.
- Report scheduling prerequisites, resource bottlenecks and solver results.
- Run a relaxed diagnostic search if the complete model cannot be scheduled; **diagnostic partial timetables are not saved**.
- Persist generated classes, student-class memberships and timetable entries using EF Core.
- Reseed the database with reproducible example school data.

## Technology

| Component | Technology |
|---|---|
| Desktop UI | WPF / C# |
| Target framework | .NET 9 (`net9.0-windows`) |
| Database | SQLite by default; EF Core 9.0.9 |
| Solver | Google OR-Tools 9.14.6206 (CP-SAT) |
| IDE | Visual Studio with .NET desktop development workload |

The project also references the EF Core SQL Server provider, although the default database connection is SQLite.

## Getting started

1. Open `JollySchoolTimetabler.csproj` (or the solution) in Visual Studio on Windows.
2. Restore NuGet packages and build the project.
3. Run the application.
4. Use the **Database** tab to reset and seed example data if needed.
5. Open the **Curriculum Configurator** to review weekly subject allocations.
6. Generate the timetable, then inspect the **Student**, **Teacher** and **Room** timetable views.

**Warning:** Database reset/reseed deletes existing school data. Back up your database first.

## Default curriculum

The seeded curriculum targets **30 periods per student per week**, across five days with six periods per day.

| Subject | Periods per week |
|---|---:|
| English | 5 |
| Mathematics | 5 |
| Science | 5 |
| History | 3 |
| Geography | 3 |
| Physical Education | 3 |
| Computing | 2 |
| Four selected non-core subjects | 1 each (4 total) |
| **Total** | **30** |

Core subjects are compulsory. Each student is seeded with four non-core subjects selected from Art, Music, Drama, Design & Technology, French, Spanish, Business Studies and Religious Education.

Curriculum requirements can be edited per year group. Changes to curriculum settings affect **subsequent timetable generation**; they do not automatically rebuild an already saved timetable.

## Database and configuration

The default SQLite connection in `Data/SchoolContext.cs` is:

```text
Data Source=school.db
```

This is a relative path, so the database location depends on the application's working directory.

Year-specific curriculum rules are stored separately at:

```text
%LOCALAPPDATA%\JollySchoolTimetabler\curriculum.json
```

Reseeding regenerates the curriculum defaults; normal startup and timetable generation use the saved configuration. The database includes an explicit `StudentClassMembership` many-to-many relationship because each student attends multiple teaching classes.

## Timetable generation

The generator builds teaching groups from student subject choices and curriculum requirements. CP-SAT then assigns weekly periods, qualified teachers and suitable rooms while enforcing resource-conflict constraints.

The current development branch also introduces coordinated optional-subject pathways and option-block scheduling to reduce clashes when students have a full 30-period week.

A successful run must pass validation before timetable entries are committed. If the solver cannot produce a complete solution, a secondary search may report how many periods it could place and which groups remain unscheduled. **These partial results are for diagnostics only and do not overwrite the saved timetable.**

### Troubleshooting

- **Infeasible timetable:** Review the diagnostic output for group sizes, eligible teachers, room capacities and missing lesson-periods. A partial result does not prove which constraint caused infeasibility.
- **Unexpected free periods:** Check the Curriculum Configurator, student subject selections and whether a new complete timetable was successfully generated and saved.
- **Room-capacity bottlenecks:** Larger groups may have far fewer eligible rooms than smaller groups.
- **Database reset:** Back up `school.db` and any custom curriculum configuration before reseeding.
- **Build errors:** Confirm the installed .NET 9 SDK, WPF desktop workload and NuGet restore.

## Project structure

```text
Data/                 EF Core context and database seeding
Models/               Students, teachers, rooms, subjects and timetable entities
Services/             Timetable generation and persistence
Services/Scheduling/  Curriculum rules, CP-SAT solver and diagnostics
ViewModels/           WPF view models and commands
Views/                Student, teacher, room, class and timetable screens
Converters/           WPF value converters
Events/               Application-wide refresh notifications
```

## Current limitations

- Finding a complete 30-period timetable can be difficult, especially with tightly constrained room sizes, teacher qualifications and option combinations.
- Diagnostic partial solutions are not valid finished timetables and are never saved as such.
- Changes to subject assignments, resources or curriculum require timetable regeneration to be reflected in scheduled lessons.
- The project has not been presented here as a production-ready or independently verified school scheduling system.

## License

No licence has been specified. Add a `LICENSE` file before distributing the project under an open-source licence.
