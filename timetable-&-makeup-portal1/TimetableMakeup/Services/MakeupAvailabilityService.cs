using Microsoft.EntityFrameworkCore;
using TimetableMakeup.Data;
using TimetableMakeup.Models;

namespace TimetableMakeup.Services;

/// <summary>
/// Computes real missed-class sessions, conflict-free makeup start blocks,
/// suitable free rooms, and approval-time block validation scoped by claims.
/// </summary>
public class MakeupAvailabilityService
{
    public const int LunchSlotNumber = 5; // 12:00 - 13:00 (never offered or crossed)
    public const int MaxSlotNumber = 9;   // 16:00 - 17:00
    public const int MaxMissedAgeDays = 120;

    private readonly AppDbContext _db;

    public MakeupAvailabilityService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the teacher's regular (non-makeup) scheduled sessions for an assignment
    /// on the weekday of missedDate, grouping consecutive hourly slots into one session.
    /// Rejects weekends, future dates, and dates older than 14 days.
    /// </summary>
    public async Task<List<MissedSessionOption>> GetMissedSessionOptionsAsync(
        string teacherId,
        string assignmentId,
        DateTime missedDate)
    {
        var date = missedDate.Date;
        var today = DateTime.Today;

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return new List<MissedSessionOption>();
        }

        if (date > today || date < today.AddDays(-MaxMissedAgeDays))
        {
            return new List<MissedSessionOption>();
        }

        var entries = await _db.TimetableEntries
            .Where(e => e.TeacherId == teacherId
                        && e.TeachingAssignmentId == assignmentId
                        && !e.IsMakeup
                        && e.DayOfWeek == date.DayOfWeek)
            .ToListAsync();

        var orderedSlots = entries
            .Select(e => e.SlotNumber)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        if (orderedSlots.Count == 0)
        {
            return new List<MissedSessionOption>();
        }

        // Group consecutive slot numbers (e.g., 1,2,3 -> StartSlot 1, Duration 3)
        var options = new List<MissedSessionOption>();
        var groupStart = orderedSlots[0];
        var groupEnd = orderedSlots[0];

        for (var i = 1; i < orderedSlots.Count; i++)
        {
            if (orderedSlots[i] == groupEnd + 1)
            {
                groupEnd = orderedSlots[i];
            }
            else
            {
                options.Add(BuildMissedSessionOption(groupStart, groupEnd));
                groupStart = orderedSlots[i];
                groupEnd = orderedSlots[i];
            }
        }

        options.Add(BuildMissedSessionOption(groupStart, groupEnd));
        return options;
    }

    /// <summary>
    /// Computes conflict-free start slots from tomorrow through Friday of next week (Mon-Fri),
    /// ensuring the full block avoids lunch slot 5, stays within slots 1..9, has no teacher or
    /// section conflict, and has at least one suitable room free for every hour of the block.
    /// </summary>
    public async Task<List<FreeStartOption>> GetFreeStartOptionsAsync(
        string programId,
        string teacherId,
        string sectionId,
        string courseId,
        int durationHours,
        DateTime today)
    {
        if (durationHours < 1 || durationHours > MaxSlotNumber)
        {
            return new List<FreeStartOption>();
        }

        var teacherExists = await _db.Teachers
            .AnyAsync(t => t.Id == teacherId && t.ProgramId == programId);
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.ProgramId == programId);
        var section = await _db.Sections
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.ProgramId == programId);

        if (!teacherExists || course is null || section is null)
        {
            return new List<FreeStartOption>();
        }

        var rooms = await _db.Rooms
            .Where(r => r.ProgramId == programId)
            .ToListAsync();

        var suitableRooms = rooms
            .Where(r => r.IsLab == course.RequiresLab && r.Capacity >= section.StudentCount)
            .ToList();

        if (suitableRooms.Count == 0)
        {
            return new List<FreeStartOption>();
        }

        // Load all program timetable entries once, then evaluate dates and blocks in memory
        var entries = await _db.TimetableEntries
            .Where(e => e.ProgramId == programId)
            .ToListAsync();

        var todayDate = today.Date;
        var startDate = todayDate.AddDays(1);
        var (thisMonday, _) = TimetableService.GetWeekRange(todayDate);
        var endDate = thisMonday.AddDays(11); // Friday of next week

        var results = new List<FreeStartOption>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            var dayEntries = entries
                .Where(e => TimetableService.AppliesOnDate(e, date))
                .ToList();

            for (var startSlot = 1; startSlot <= MaxSlotNumber; startSlot++)
            {
                if (!IsValidBlockRange(startSlot, durationHours))
                {
                    continue;
                }

                var blockSlots = Enumerable.Range(startSlot, durationHours).ToArray();

                // Teacher must be free across the entire block
                if (!BlockFree(dayEntries, blockSlots, e => e.TeacherId == teacherId, date))
                {
                    continue;
                }

                // Section must be free across the entire block
                if (!BlockFree(dayEntries, blockSlots, e => e.SectionId == sectionId, date))
                {
                    continue;
                }

                // Count suitable rooms that are free for every hour in the block
                var freeRoomCount = suitableRooms.Count(room =>
                    BlockFree(dayEntries, blockSlots, e => e.RoomId == room.Id, date));

                if (freeRoomCount > 0)
                {
                    results.Add(new FreeStartOption(date, startSlot, durationHours, freeRoomCount));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Returns suitable rooms (matching lab requirement and adequate capacity) that are free
    /// across every slot of the requested block, ordered by smallest adequate capacity first.
    /// </summary>
    public async Task<List<Room>> GetFreeRoomsForBlockAsync(
        string programId,
        string courseId,
        string sectionId,
        DateTime date,
        int startSlot,
        int hours)
    {
        var targetDate = date.Date;
        if (targetDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return new List<Room>();
        }

        if (!IsValidBlockRange(startSlot, hours))
        {
            return new List<Room>();
        }

        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.ProgramId == programId);
        var section = await _db.Sections
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.ProgramId == programId);

        if (course is null || section is null)
        {
            return new List<Room>();
        }

        var suitableRooms = await _db.Rooms
            .Where(r => r.ProgramId == programId
                        && r.IsLab == course.RequiresLab
                        && r.Capacity >= section.StudentCount)
            .ToListAsync();

        if (suitableRooms.Count == 0)
        {
            return new List<Room>();
        }

        var entries = await _db.TimetableEntries
            .Where(e => e.ProgramId == programId && e.DayOfWeek == targetDate.DayOfWeek)
            .ToListAsync();

        var blockSlots = Enumerable.Range(startSlot, hours).ToArray();

        return suitableRooms
            .Where(room => BlockFree(entries, blockSlots, e => e.RoomId == room.Id, targetDate))
            .OrderBy(r => r.Capacity)
            .ThenBy(r => r.Code)
            .ToList();
    }

    /// <summary>
    /// Approval-time re-check verifying that the block does not cross lunch or slot 9,
    /// room belongs to program and fits capacity (and lab type if courseId is supplied),
    /// and teacher, section, and room have no active conflict on any slot of the block.
    /// </summary>
    public async Task<(bool Ok, string? Error)> CheckBlockAsync(
        string programId,
        string teacherId,
        string sectionId,
        string roomId,
        DateTime date,
        int startSlot,
        int hours,
        string? courseId = null)
    {
        var targetDate = date.Date;

        if (targetDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return (false, "Makeup classes cannot be scheduled on weekends.");
        }

        if (hours < 1 || startSlot < 1 || startSlot + hours - 1 > MaxSlotNumber)
        {
            return (false, $"Selected block (Slot {startSlot}, {hours}h) goes outside standard slots (1-{MaxSlotNumber}).");
        }

        var blockSlots = Enumerable.Range(startSlot, hours).ToArray();
        if (blockSlots.Contains(LunchSlotNumber))
        {
            return (false, "Makeup block cannot use or cross lunch slot 5 (12:00-13:00).");
        }

        var room = await _db.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId && r.ProgramId == programId);
        if (room is null)
        {
            return (false, "Selected room does not belong to your program.");
        }

        var section = await _db.Sections
            .FirstOrDefaultAsync(s => s.Id == sectionId && s.ProgramId == programId);
        if (section is null)
        {
            return (false, "Selected section does not belong to your program.");
        }

        if (room.Capacity < section.StudentCount)
        {
            return (false, $"Room {room.Code} capacity ({room.Capacity}) is below section size ({section.StudentCount}).");
        }

        if (!string.IsNullOrEmpty(courseId))
        {
            var course = await _db.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.ProgramId == programId);
            if (course is null)
            {
                return (false, "Selected course does not belong to your program.");
            }

            if (room.IsLab != course.RequiresLab)
            {
                var expected = course.RequiresLab ? "a lab room" : "a non-lab classroom";
                return (false, $"Course {course.Code} requires {expected}, which {room.Code} does not match.");
            }
        }

        var entries = await _db.TimetableEntries
            .Where(e => e.ProgramId == programId && e.DayOfWeek == targetDate.DayOfWeek)
            .ToListAsync();

        if (!BlockFree(entries, blockSlots, e => e.TeacherId == teacherId, targetDate))
        {
            return (false, $"Conflict detected: The teacher already has a class during that block on {targetDate:ddd, dd MMM yyyy}.");
        }

        if (!BlockFree(entries, blockSlots, e => e.SectionId == sectionId, targetDate))
        {
            return (false, $"Conflict detected: The section already has a class during that block on {targetDate:ddd, dd MMM yyyy}.");
        }

        if (!BlockFree(entries, blockSlots, e => e.RoomId == roomId, targetDate))
        {
            return (false, $"Conflict detected: Room {room.Code} is already occupied during that block on {targetDate:ddd, dd MMM yyyy}.");
        }

        return (true, null);
    }
/// <summary>Returns why a block cannot be approved, or null if it is fine.</summary>
public async Task<string?> GetBlockProblemAsync(
    string programId, string teacherId, string sectionId, string courseId,
    DateTime date, int startSlot, int hours)
{
    var targetDate = date.Date;
    if (!IsValidBlockRange(startSlot, hours))
        return "The block crosses lunch or goes outside 08:00-17:00.";

    var blockSlots = Enumerable.Range(startSlot, hours).ToArray();
    var entries = await _db.TimetableEntries
        .Where(e => e.ProgramId == programId && e.DayOfWeek == targetDate.DayOfWeek)
        .ToListAsync();

    if (!BlockFree(entries, blockSlots, e => e.TeacherId == teacherId, targetDate))
        return "The teacher already has a class at that time.";

    if (!BlockFree(entries, blockSlots, e => e.SectionId == sectionId, targetDate))
        return "The section already has a class at that time (for example another approved makeup).";

    var rooms = await GetFreeRoomsForBlockAsync(programId, courseId, sectionId, date, startSlot, hours);
    if (rooms.Count == 0)
        return "No suitable room is free for the whole block.";

    return null;
}
    // =========================================================================
    // Helpers
    // =========================================================================

    public static bool IsValidBlockRange(int startSlot, int hours)
    {
        if (hours < 1 || startSlot < 1 || startSlot + hours - 1 > MaxSlotNumber)
        {
            return false;
        }

        for (var s = startSlot; s < startSlot + hours; s++)
        {
            if (s == LunchSlotNumber)
            {
                return false;
            }
        }

        return true;
    }

    private static bool BlockFree(
        IEnumerable<TimetableEntry> entries,
        int[] blockSlots,
        Func<TimetableEntry, bool> mine,
        DateTime date) =>
        !entries.Any(e => blockSlots.Contains(e.SlotNumber)
                          && TimetableService.AppliesOnDate(e, date)
                          && mine(e));

    private static MissedSessionOption BuildMissedSessionOption(int startSlot, int endSlot)
    {
        var duration = endSlot - startSlot + 1;
        var startDef = TimetableService.StandardSlots.FirstOrDefault(s => s.SlotNumber == startSlot);
        var endDef = TimetableService.StandardSlots.FirstOrDefault(s => s.SlotNumber == endSlot);

        var timeRange = startDef is not null && endDef is not null
            ? $"{startDef.StartTime}-{endDef.EndTime}"
            : string.Empty;

        var slotText = duration == 1
            ? $"Slot {startSlot}"
            : $"Slot {startSlot}-{endSlot}";

        var label = string.IsNullOrEmpty(timeRange)
            ? slotText
            : $"{slotText} ({timeRange})";

        return new MissedSessionOption(startSlot, duration, label);
    }
}

public record MissedSessionOption(int StartSlot, int DurationHours, string Label);

public record FreeStartOption(DateTime Date, int StartSlot, int Hours, int FreeRoomCount);
