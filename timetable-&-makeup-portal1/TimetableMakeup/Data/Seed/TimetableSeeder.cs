using Microsoft.EntityFrameworkCore;
using TimetableMakeup.Models;
using TimetableMakeup.Services;

namespace TimetableMakeup.Data.Seed;

/// <summary>
/// Builds teachers, courses, rooms, sections, assignments, timetable entries and
/// logins from TimetableSeedData. Runs only when the database is empty.
/// </summary>
public static class TimetableSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Departments.AnyAsync())
        {
            logger.LogInformation("Seed skipped: data already exists.");
            return;
        }

        var sessions = TimetableSeedData.Sessions;
        var slots = TimetableService.StandardSlots;

        // ---- Department and one shared program (CS + IT, Shift-I) ----
        var dept = new Department { Code = "CS-IT", Name = "Department of Computer Science & IT" };
        var program = new AcademicProgram { DepartmentId = dept.Id, Code = "CS-IT", Name = "BSCS / BSIT (Shift-I)" };
        var pid = program.Id;

        // ---- Sections ----
        var sections = TimetableSeedData.Sections.ToDictionary(s => s.Name, s => new Section
        {
            ProgramId = pid, Name = s.Name, Semester = s.Semester, Shift = "Morning", StudentCount = 40
        });

        // ---- Teachers ----
        var teachers = new Dictionary<string, Teacher>();
        var emails = new HashSet<string>();
        int n = 1;
        foreach (var name in sessions.Select(s => s.Teacher).Distinct().OrderBy(x => x))
        {
            var email = MakeEmail(name);
            if (!emails.Add(email)) email = email.Replace("@", $"{n}@");
            teachers[name] = new Teacher
            {
                ProgramId = pid, EmployeeCode = $"EMP-{n++:000}", FullName = name,
                Email = email, Designation = "Faculty", Specialization = string.Empty
            };
        }

        // ---- Courses (one per distinct title; "... Lab" requires a lab) ----
        var courses = new Dictionary<string, Course>();
        int c = 1;
        foreach (var title in sessions.Select(s => s.Course).Distinct().OrderBy(x => x))
        {
            bool isLab = title.EndsWith(" Lab");
            courses[title] = new Course
            {
                ProgramId = pid, Code = $"CRS-{c++:000}", Title = title,
                CreditHours = title.Contains("Advisory") ? 0 : (isLab ? 1 : 3), RequiresLab = isLab
            };
        }

        // ---- Rooms ----
        var rooms = new Dictionary<string, Room>();
        foreach (var name in sessions.Select(s => s.Room).Distinct().OrderBy(x => x))
        {
            bool isLab = name.Contains("Lab");
            rooms[name] = new Room
            {
                ProgramId = pid, Code = name, Name = name, IsLab = isLab, Capacity = isLab ? 40 : 50
            };
        }

        // ---- Teaching assignments (unique teacher + course + section) ----
        var assignments = new Dictionary<string, TeachingAssignment>();
        foreach (var s in sessions)
        {
            var key = $"{s.Teacher}|{s.Course}|{s.Section}";
            if (assignments.ContainsKey(key)) continue;
            assignments[key] = new TeachingAssignment
            {
                ProgramId = pid, TeacherId = teachers[s.Teacher].Id, CourseId = courses[s.Course].Id,
                SectionId = sections[s.Section].Id, SemesterTerm = "Fall 2026"
            };
        }

        // ---- Timetable entries (a multi-hour session becomes several hourly entries) ----
        var entries = new List<TimetableEntry>();
        foreach (var s in sessions)
        {
            var a = assignments[$"{s.Teacher}|{s.Course}|{s.Section}"];
            for (int h = 0; h < s.Hours; h++)
            {
                int slot = s.Slot + h;
                if (slot < 1 || slot > slots.Count)
                {
                    logger.LogWarning("Seed error: {S} {D} slot {Slot} is outside the grid.", s.Section, s.Day, slot);
                    continue;
                }
                entries.Add(new TimetableEntry
                {
                    ProgramId = pid, TeachingAssignmentId = a.Id, TeacherId = a.TeacherId,
                    CourseId = a.CourseId, SectionId = a.SectionId, RoomId = rooms[s.Room].Id,
                    DayOfWeek = s.Day, SlotNumber = slot,
                    StartTime = slots[slot - 1].StartTime, EndTime = slots[slot - 1].EndTime,
                    IsMakeup = false
                });
            }
        }
        WarnOnConflicts(entries, teachers, rooms, sections, logger);

        // ---- Users ----
        var users = new List<AppUser>
        {
            new() { FullName = "Prof. Bilal Ahmed", Email = "coordinator@demo.edu",
                    Password = "Coord@123", Role = Role.Coordinator, ProgramId = pid }
        };
        foreach (var t in teachers.Values)
            users.Add(new AppUser { FullName = t.FullName, Email = t.Email, Password = "Teach@123",
                                    Role = Role.Teacher, ProgramId = pid, TeacherId = t.Id });

        // Shortcut login for demos: teacher@demo.edu = Ms. Faryal Farooq
        var demoTeacher = teachers["Ms. Faryal Farooq"];
        users.Add(new AppUser { FullName = demoTeacher.FullName, Email = "teacher@demo.edu",
                                Password = "Teach@123", Role = Role.Teacher,
                                ProgramId = pid, TeacherId = demoTeacher.Id });

        // ---- Save ----
        db.Departments.Add(dept);
        db.AcademicPrograms.Add(program);
        db.Sections.AddRange(sections.Values);
        db.Teachers.AddRange(teachers.Values);
        db.Courses.AddRange(courses.Values);
        db.Rooms.AddRange(rooms.Values);
        db.TeachingAssignments.AddRange(assignments.Values);
        db.TimetableEntries.AddRange(entries);
        db.AppUsers.AddRange(users);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seeded: {Sec} sections, {T} teachers, {C} courses, {R} rooms, {A} assignments, {E} timetable entries, {U} users.",
            sections.Count, teachers.Count, courses.Count, rooms.Count, assignments.Count, entries.Count, users.Count);
    }

    // "Ms. Faryal Farooq" -> "faryal.farooq@demo.edu" (always lowercase: login lowercases input)
    private static string MakeEmail(string fullName)
    {
        var name = fullName;
        foreach (var title in new[] { "Dr. ", "Mr. ", "Ms. " })
            if (name.StartsWith(title)) name = name[title.Length..];

        var chars = name.ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ch == ' ' ? '.' : ch == '-' ? '-' : '\0')
            .Where(ch => ch != '\0');
        return new string(chars.ToArray()) + "@demo.edu";
    }

    // Logs a warning if a teacher, room or section is double-booked.
    private static void WarnOnConflicts(List<TimetableEntry> entries,
        Dictionary<string, Teacher> teachers, Dictionary<string, Room> rooms,
        Dictionary<string, Section> sections, ILogger logger)
    {
        void Check(string label, Func<TimetableEntry, string> key, Func<string, string> display)
        {
            foreach (var g in entries.GroupBy(e => (Id: key(e), e.DayOfWeek, e.SlotNumber)).Where(g => g.Count() > 1))
                logger.LogWarning("Seed conflict ({Label}): {Who} on {Day} slot {Slot}",
                    label, display(g.Key.Id), g.Key.DayOfWeek, g.Key.SlotNumber);
        }
        Check("teacher", e => e.TeacherId, id => teachers.Values.First(t => t.Id == id).FullName);
        Check("room",    e => e.RoomId,    id => rooms.Values.First(r => r.Id == id).Name);
        Check("section", e => e.SectionId, id => sections.Values.First(s => s.Id == id).Name);
    }
}
