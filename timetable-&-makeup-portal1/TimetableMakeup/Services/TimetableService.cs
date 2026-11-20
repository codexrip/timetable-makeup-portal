using Microsoft.EntityFrameworkCore;
using TimetableMakeup.Data;
using TimetableMakeup.Models;

namespace TimetableMakeup.Services;

/// <summary>
/// Queries timetable, course assignments, faculty, and dashboard statistics strictly scoped by claims.
/// Regular entries repeat weekly; makeup entries apply only on their specific MakeupDate.
/// </summary>
public class TimetableService
{
    private readonly AppDbContext _db;

    public TimetableService(AppDbContext db)
    {
        _db = db;
    }

    public static IReadOnlyList<TimeSlotDefinition> StandardSlots { get; } = new List<TimeSlotDefinition>
    {
        new(1, "08:00", "09:00"),
        new(2, "09:00", "10:00"),
        new(3, "10:00", "11:00"),
        new(4, "11:00", "12:00"),
        new(5, "12:00", "13:00"),
        new(6, "13:00", "14:00"),
        new(7, "14:00", "15:00"),
        new(8, "15:00", "16:00"),
        new(9, "16:00", "17:00")
    };

    public static (DateTime WeekStart, DateTime WeekEnd) GetWeekRange(DateTime referenceDate)
    {
        var date = referenceDate.Date;
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var weekStart = date.AddDays(-diff);
        var weekEnd = weekStart.AddDays(6);
        return (weekStart, weekEnd);
    }

    public static List<TimetableEntry> FilterEntriesForWeek(
        IEnumerable<TimetableEntry> entries,
        DateTime weekReferenceDate)
    {
        var (weekStart, weekEnd) = GetWeekRange(weekReferenceDate);
        return entries
            .Where(e => !e.IsMakeup
                        || (e.MakeupDate.HasValue
                            && e.MakeupDate.Value.Date >= weekStart
                            && e.MakeupDate.Value.Date <= weekEnd))
            .ToList();
    }

    public static bool AppliesOnDate(TimetableEntry entry, DateTime date)
    {
        if (entry.DayOfWeek != date.DayOfWeek)
        {
            return false;
        }

        if (!entry.IsMakeup)
        {
            return true;
        }

        return entry.MakeupDate.HasValue && entry.MakeupDate.Value.Date == date.Date;
    }

    // =========================================================================
    // Teacher Queries (scoped strictly by TeacherId from claims)
    // =========================================================================

    public async Task<TeacherDashboardStats> GetTeacherDashboardStatsAsync(string teacherId)
    {
        var assignments = await _db.TeachingAssignments
            .Where(a => a.TeacherId == teacherId)
            .ToListAsync();

        var assignedCourses = assignments.Select(a => a.CourseId).Distinct().Count();
        var assignedSections = assignments.Select(a => a.SectionId).Distinct().Count();

        var today = DateTime.Today;
        var teacherEntries = await _db.TimetableEntries
            .Where(e => e.TeacherId == teacherId)
            .ToListAsync();

        var todaysClasses = teacherEntries.Count(e => AppliesOnDate(e, today));

        var requests = await _db.MakeupRequests
            .Where(r => r.TeacherId == teacherId)
            .ToListAsync();

        var pending = requests.Count(r => r.Status == MakeupRequestStatus.Pending);
        var approved = requests.Count(r => r.Status == MakeupRequestStatus.Approved);
        var reviewRequested = requests.Count(r => r.Status == MakeupRequestStatus.ReviewRequested);
        var held = requests.Count(r => r.Status == MakeupRequestStatus.Held);

        return new TeacherDashboardStats(
            assignedCourses,
            assignedSections,
            todaysClasses,
            pending,
            approved,
            reviewRequested,
            held);
    }

    public async Task<List<UpcomingMakeupView>> GetTeacherUpcomingMakeupsAsync(
        string teacherId,
        DateTime fromDate,
        int days = 14)
    {
        var start = fromDate.Date;
        var end = start.AddDays(days);

        var approvedRequests = await _db.MakeupRequests
            .Where(r => r.TeacherId == teacherId && r.Status == MakeupRequestStatus.Approved)
            .ToListAsync();

        var upcoming = approvedRequests
            .Where(r => r.ApprovedDate.HasValue
                        && r.ApprovedDate.Value.Date >= start
                        && r.ApprovedDate.Value.Date <= end)
            .OrderBy(r => r.ApprovedDate!.Value)
            .ThenBy(r => r.ApprovedSlotNumber ?? 0)
            .ToList();

        return await BuildUpcomingMakeupViewsAsync(upcoming);
    }

    public async Task<List<TeacherCourseView>> GetTeacherCoursesAsync(string teacherId)
    {
        var assignments = await _db.TeachingAssignments
            .Where(a => a.TeacherId == teacherId)
            .ToListAsync();

        if (assignments.Count == 0)
        {
            return new List<TeacherCourseView>();
        }

        var courseIds = assignments.Select(a => a.CourseId).Distinct().ToList();
        var sectionIds = assignments.Select(a => a.SectionId).Distinct().ToList();

        var courses = await _db.Courses
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var sections = await _db.Sections
            .Where(s => sectionIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        var entries = await _db.TimetableEntries
            .Where(e => e.TeacherId == teacherId)
            .ToListAsync();

        return assignments
            .Select(a =>
            {
                courses.TryGetValue(a.CourseId, out var course);
                sections.TryGetValue(a.SectionId, out var section);
                var weeklySlots = entries.Count(e => e.TeachingAssignmentId == a.Id && !e.IsMakeup);

                return new TeacherCourseView(
                    AssignmentId: a.Id,
                    CourseId: a.CourseId,
                    CourseCode: course?.Code ?? "N/A",
                    CourseTitle: course?.Title ?? "Unknown Course",
                    CreditHours: course?.CreditHours ?? 0,
                    RequiresLab: course?.RequiresLab ?? false,
                    SectionId: a.SectionId,
                    SectionName: section?.Name ?? "Unknown Section",
                    SemesterTerm: a.SemesterTerm,
                    WeeklyLectures: weeklySlots);
            })
            .OrderBy(x => x.CourseCode)
            .ThenBy(x => x.SectionName)
            .ToList();
    }

    public async Task<List<TimetableSlotView>> GetTeacherTimetableAsync(
        string teacherId,
        DateTime? weekReferenceDate = null)
    {
        var entries = await _db.TimetableEntries
            .Where(e => e.TeacherId == teacherId)
            .ToListAsync();

        var filtered = FilterEntriesForWeek(entries, weekReferenceDate ?? DateTime.Today);
        return await BuildSlotViewsAsync(filtered);
    }

    // =========================================================================
    // Coordinator Queries (scoped strictly by ProgramId from claims)
    // =========================================================================

    public async Task<CoordinatorDashboardStats> GetCoordinatorDashboardStatsAsync(string programId)
    {
        var program = await _db.AcademicPrograms.FirstOrDefaultAsync(p => p.Id == programId);
        var teachersCount = await _db.Teachers.Where(t => t.ProgramId == programId).CountAsync();
        var coursesCount = await _db.Courses.Where(c => c.ProgramId == programId).CountAsync();
        var sectionsCount = await _db.Sections.Where(s => s.ProgramId == programId).CountAsync();
        var rooms = await _db.Rooms.Where(r => r.ProgramId == programId).ToListAsync();

        var requests = await _db.MakeupRequests
            .Where(r => r.ProgramId == programId)
            .ToListAsync();

        return new CoordinatorDashboardStats(
            ProgramCode: program?.Code ?? "Program",
            ProgramName: program?.Name ?? string.Empty,
            TeachersCount: teachersCount,
            CoursesCount: coursesCount,
            SectionsCount: sectionsCount,
            RoomsCount: rooms.Count,
            LabsCount: rooms.Count(r => r.IsLab),
            PendingRequestsCount: requests.Count(r => r.Status == MakeupRequestStatus.Pending),
            ApprovedRequestsCount: requests.Count(r => r.Status == MakeupRequestStatus.Approved));
    }

    public async Task<CoordinatorDashboardData> GetCoordinatorDashboardDataAsync(string programId, DateTime today)
    {
        var todayDate = today.Date;
        var (weekStart, weekEnd) = GetWeekRange(todayDate);
        var twoDaysAgoUtc = DateTime.UtcNow.AddDays(-2);

        var program = await _db.AcademicPrograms.FirstOrDefaultAsync(p => p.Id == programId);
        var teachers = await _db.Teachers
            .Where(t => t.ProgramId == programId)
            .OrderBy(t => t.FullName)
            .ToListAsync();
        var sections = await _db.Sections
            .Where(s => s.ProgramId == programId)
            .ToListAsync();
        var courses = await _db.Courses
            .Where(c => c.ProgramId == programId)
            .OrderBy(c => c.Code)
            .ToListAsync();
        var entries = await _db.TimetableEntries
            .Where(e => e.ProgramId == programId)
            .ToListAsync();
        var requests = await _db.MakeupRequests
            .Where(r => r.ProgramId == programId)
            .ToListAsync();

        var totalStudents = sections.Sum(s => s.StudentCount);
        var weeklyRegularClasses = entries.Count(e => !e.IsMakeup);
        var makeupsThisWeek = requests.Count(r =>
            (r.Status == MakeupRequestStatus.Approved || r.Status == MakeupRequestStatus.Held)
            && r.ApprovedDate.HasValue
            && r.ApprovedDate.Value.Date >= weekStart
            && r.ApprovedDate.Value.Date <= weekEnd);

        var pendingRequests = requests.Count(r => r.Status == MakeupRequestStatus.Pending);
        var waitingOverTwoDays = requests.Count(r =>
            r.Status == MakeupRequestStatus.Pending
            && r.CreatedAt <= twoDaysAgoUtc);

        var upcomingEnd = todayDate.AddDays(7);
        var upcomingRequests = requests
            .Where(r => r.Status == MakeupRequestStatus.Approved
                        && r.ApprovedDate.HasValue
                        && r.ApprovedDate.Value.Date >= todayDate
                        && r.ApprovedDate.Value.Date <= upcomingEnd)
            .OrderBy(r => r.ApprovedDate!.Value)
            .ThenBy(r => r.ApprovedSlotNumber ?? 0)
            .ToList();

        var upcomingMakeups = await BuildUpcomingMakeupViewsAsync(upcomingRequests);

        var workloadRows = teachers
            .Select(t =>
            {
                var teacherReqs = requests.Where(r => r.TeacherId == t.Id).ToList();
                return new TeacherWorkloadRow(
                    TeacherName: t.FullName,
                    EmployeeCode: t.EmployeeCode,
                    WeeklyTeachingHours: entries.Count(e => e.TeacherId == t.Id && !e.IsMakeup),
                    MakeupRequestsCount: teacherReqs.Count,
                    HeldCount: teacherReqs.Count(r => r.Status == MakeupRequestStatus.Held),
                    NotHeldCount: teacherReqs.Count(r => r.Status == MakeupRequestStatus.NotHeld));
            })
            .OrderByDescending(w => w.MakeupRequestsCount)
            .ThenByDescending(w => w.WeeklyTeachingHours)
            .ThenBy(w => w.TeacherName)
            .ToList();

        var makeupsByCourse = courses
            .Select(c =>
            {
                var courseReqs = requests.Where(r => r.CourseId == c.Id).ToList();
                return new CourseMakeupRow(
                    CourseCode: c.Code,
                    CourseTitle: c.Title,
                    RequestsCount: courseReqs.Count,
                    HeldCount: courseReqs.Count(r => r.Status == MakeupRequestStatus.Held));
            })
            .Where(row => row.RequestsCount > 0)
            .OrderByDescending(row => row.RequestsCount)
            .ThenBy(row => row.CourseCode)
            .ToList();

        return new CoordinatorDashboardData(
            ProgramCode: program?.Code ?? "Program",
            ProgramName: program?.Name ?? string.Empty,
            TeachersCount: teachers.Count,
            SectionsCount: sections.Count,
            TotalStudentsCount: totalStudents,
            CoursesCount: courses.Count,
            WeeklyRegularClassesCount: weeklyRegularClasses,
            MakeupsThisWeekCount: makeupsThisWeek,
            PendingRequestsCount: pendingRequests,
            WaitingOverTwoDaysCount: waitingOverTwoDays,
            UpcomingMakeups: upcomingMakeups,
            TeacherWorkloads: workloadRows,
            MakeupsByCourse: makeupsByCourse);
    }

    private async Task<List<UpcomingMakeupView>> BuildUpcomingMakeupViewsAsync(List<MakeupRequest> requests)
    {
        if (requests.Count == 0)
        {
            return new List<UpcomingMakeupView>();
        }

        var courseIds = requests.Select(r => r.CourseId).Distinct().ToList();
        var sectionIds = requests.Select(r => r.SectionId).Distinct().ToList();
        var teacherIds = requests.Select(r => r.TeacherId).Distinct().ToList();
        var roomIds = requests
            .Where(r => !string.IsNullOrEmpty(r.ApprovedRoomId))
            .Select(r => r.ApprovedRoomId!)
            .Distinct()
            .ToList();

        var courses = await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        var sections = await _db.Sections.Where(s => sectionIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);
        var teachers = await _db.Teachers.Where(t => teacherIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        var rooms = roomIds.Count > 0
            ? await _db.Rooms.Where(r => roomIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id)
            : new Dictionary<string, Room>();

        return requests.Select(r =>
        {
            courses.TryGetValue(r.CourseId, out var course);
            sections.TryGetValue(r.SectionId, out var section);
            teachers.TryGetValue(r.TeacherId, out var teacher);
            Room? room = null;
            if (!string.IsNullOrEmpty(r.ApprovedRoomId))
            {
                rooms.TryGetValue(r.ApprovedRoomId, out room);
            }

            var startSlot = r.ApprovedSlotNumber ?? 1;
            var hours = r.ApprovedDurationHours ?? (r.DurationHours > 0 ? r.DurationHours : 1);
            var endSlot = startSlot + hours - 1;
            var startDef = StandardSlots.FirstOrDefault(s => s.SlotNumber == startSlot);
            var endDef = StandardSlots.FirstOrDefault(s => s.SlotNumber == endSlot);
            var timeRange = startDef is not null && endDef is not null
                ? $"{startDef.StartTime}-{endDef.EndTime}"
                : $"Slot {startSlot}";

            return new UpcomingMakeupView(
                RequestId: r.Id,
                Date: r.ApprovedDate!.Value.Date,
                TimeRange: timeRange,
                CourseCode: course?.Code ?? "N/A",
                CourseTitle: course?.Title ?? "Unknown Course",
                SectionName: section?.Name ?? "N/A",
                TeacherName: teacher?.FullName ?? "Unknown Teacher",
                RoomCode: room?.Code ?? "N/A");
        }).ToList();
    }

    public async Task<List<FacultyMemberView>> GetProgramFacultyAsync(string programId)
    {
        var teachers = await _db.Teachers
            .Where(t => t.ProgramId == programId)
            .OrderBy(t => t.FullName)
            .ToListAsync();

        if (teachers.Count == 0)
        {
            return new List<FacultyMemberView>();
        }

        var assignments = await _db.TeachingAssignments
            .Where(a => a.ProgramId == programId)
            .ToListAsync();

        var courses = await _db.Courses
            .Where(c => c.ProgramId == programId)
            .ToDictionaryAsync(c => c.Id);

        var sections = await _db.Sections
            .Where(s => s.ProgramId == programId)
            .ToDictionaryAsync(s => s.Id);

        return teachers.Select(t =>
        {
            var teacherAssignments = assignments
                .Where(a => a.TeacherId == t.Id)
                .Select(a =>
                {
                    courses.TryGetValue(a.CourseId, out var course);
                    sections.TryGetValue(a.SectionId, out var section);
                    return new FacultyCourseAssignmentView(
                        CourseCode: course?.Code ?? "N/A",
                        CourseTitle: course?.Title ?? "Unknown",
                        SectionName: section?.Name ?? "N/A",
                        CreditHours: course?.CreditHours ?? 0);
                })
                .OrderBy(x => x.CourseCode)
                .ThenBy(x => x.SectionName)
                .ToList();

            return new FacultyMemberView(
                TeacherId: t.Id,
                EmployeeCode: t.EmployeeCode,
                FullName: t.FullName,
                Email: t.Email,
                Designation: t.Designation,
                Specialization: t.Specialization,
                Assignments: teacherAssignments);
        }).ToList();
    }

    public async Task<List<Teacher>> GetProgramTeachersAsync(string programId) =>
        await _db.Teachers
            .Where(t => t.ProgramId == programId)
            .OrderBy(t => t.FullName)
            .ToListAsync();

    public async Task<List<Section>> GetProgramSectionsAsync(string programId) =>
        await _db.Sections
            .Where(s => s.ProgramId == programId)
            .OrderBy(s => s.Semester)
            .ThenBy(s => s.Name)
            .ToListAsync();

    public async Task<List<Room>> GetProgramRoomsAsync(string programId) =>
        await _db.Rooms
            .Where(r => r.ProgramId == programId)
            .OrderBy(r => r.IsLab)
            .ThenBy(r => r.Code)
            .ToListAsync();

    public async Task<List<TimetableSlotView>> GetProgramTeacherTimetableAsync(
        string programId,
        string teacherId,
        DateTime? weekReferenceDate = null)
    {
        var entries = await _db.TimetableEntries
            .Where(e => e.ProgramId == programId && e.TeacherId == teacherId)
            .ToListAsync();

        var filtered = FilterEntriesForWeek(entries, weekReferenceDate ?? DateTime.Today);
        return await BuildSlotViewsAsync(filtered);
    }

    public async Task<List<TimetableSlotView>> GetProgramSectionTimetableAsync(
        string programId,
        string sectionId,
        DateTime? weekReferenceDate = null)
    {
        var entries = await _db.TimetableEntries
            .Where(e => e.ProgramId == programId && e.SectionId == sectionId)
            .ToListAsync();

        var filtered = FilterEntriesForWeek(entries, weekReferenceDate ?? DateTime.Today);
        return await BuildSlotViewsAsync(filtered);
    }

    public async Task<List<TimetableSlotView>> GetProgramRoomTimetableAsync(
        string programId,
        string roomId,
        DateTime? weekReferenceDate = null)
    {
        var entries = await _db.TimetableEntries
            .Where(e => e.ProgramId == programId && e.RoomId == roomId)
            .ToListAsync();

        var filtered = FilterEntriesForWeek(entries, weekReferenceDate ?? DateTime.Today);
        return await BuildSlotViewsAsync(filtered);
    }

    // =========================================================================
    // Shared Projection Helper
    // =========================================================================

    public async Task<List<TimetableSlotView>> BuildSlotViewsAsync(List<TimetableEntry> entries)
    {
        if (entries.Count == 0)
        {
            return new List<TimetableSlotView>();
        }

        var courseIds = entries.Select(e => e.CourseId).Distinct().ToList();
        var sectionIds = entries.Select(e => e.SectionId).Distinct().ToList();
        var roomIds = entries.Select(e => e.RoomId).Distinct().ToList();
        var teacherIds = entries.Select(e => e.TeacherId).Distinct().ToList();

        var courses = await _db.Courses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        var sections = await _db.Sections.Where(s => sectionIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);
        var rooms = await _db.Rooms.Where(r => roomIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id);
        var teachers = await _db.Teachers.Where(t => teacherIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);

        return entries
            .Select(e =>
            {
                courses.TryGetValue(e.CourseId, out var course);
                sections.TryGetValue(e.SectionId, out var section);
                rooms.TryGetValue(e.RoomId, out var room);
                teachers.TryGetValue(e.TeacherId, out var teacher);

                return new TimetableSlotView(
                    EntryId: e.Id,
                    DayOfWeek: e.DayOfWeek,
                    SlotNumber: e.SlotNumber,
                    StartTime: e.StartTime,
                    EndTime: e.EndTime,
                    CourseCode: course?.Code ?? "N/A",
                    CourseTitle: course?.Title ?? "Unknown",
                    SectionName: section?.Name ?? "N/A",
                    RoomCode: room?.Code ?? "N/A",
                    TeacherName: teacher?.FullName ?? "N/A",
                    IsMakeup: e.IsMakeup,
                    MakeupDate: e.MakeupDate);
            })
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.SlotNumber)
            .ToList();
    }
}

public record TimeSlotDefinition(int SlotNumber, string StartTime, string EndTime)
{
    public string Label => $"{StartTime} - {EndTime}";
}

public record TeacherDashboardStats(
    int AssignedCourses,
    int AssignedSections,
    int TodaysClasses,
    int PendingRequests,
    int ApprovedRequests,
    int ReviewRequestedRequests,
    int HeldRequests);

public record CoordinatorDashboardStats(
    string ProgramCode,
    string ProgramName,
    int TeachersCount,
    int CoursesCount,
    int SectionsCount,
    int RoomsCount,
    int LabsCount,
    int PendingRequestsCount,
    int ApprovedRequestsCount);

public record FacultyMemberView(
    string TeacherId,
    string EmployeeCode,
    string FullName,
    string Email,
    string Designation,
    string Specialization,
    List<FacultyCourseAssignmentView> Assignments);

public record FacultyCourseAssignmentView(
    string CourseCode,
    string CourseTitle,
    string SectionName,
    int CreditHours);

public record TeacherCourseView(
    string AssignmentId,
    string CourseId,
    string CourseCode,
    string CourseTitle,
    int CreditHours,
    bool RequiresLab,
    string SectionId,
    string SectionName,
    string SemesterTerm,
    int WeeklyLectures);

public record TimetableSlotView(
    string EntryId,
    DayOfWeek DayOfWeek,
    int SlotNumber,
    string StartTime,
    string EndTime,
    string CourseCode,
    string CourseTitle,
    string SectionName,
    string RoomCode,
    string TeacherName,
    bool IsMakeup,
    DateTime? MakeupDate);

public record UpcomingMakeupView(
    string RequestId,
    DateTime Date,
    string TimeRange,
    string CourseCode,
    string CourseTitle,
    string SectionName,
    string TeacherName,
    string RoomCode);

public record TeacherWorkloadRow(
    string TeacherName,
    string EmployeeCode,
    int WeeklyTeachingHours,
    int MakeupRequestsCount,
    int HeldCount,
    int NotHeldCount);

public record CourseMakeupRow(
    string CourseCode,
    string CourseTitle,
    int RequestsCount,
    int HeldCount);

public record CoordinatorDashboardData(
    string ProgramCode,
    string ProgramName,
    int TeachersCount,
    int SectionsCount,
    int TotalStudentsCount,
    int CoursesCount,
    int WeeklyRegularClassesCount,
    int MakeupsThisWeekCount,
    int PendingRequestsCount,
    int WaitingOverTwoDaysCount,
    List<UpcomingMakeupView> UpcomingMakeups,
    List<TeacherWorkloadRow> TeacherWorkloads,
    List<CourseMakeupRow> MakeupsByCourse);
