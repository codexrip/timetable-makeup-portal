using Microsoft.EntityFrameworkCore;
using TimetableMakeup.Data;
using TimetableMakeup.Models;

namespace TimetableMakeup.Services;

/// <summary>
/// Manages teacher and coordinator makeup class requests filtered strictly by claims.
/// </summary>
public class MakeupService
{
    private readonly AppDbContext _db;
    private readonly MakeupAvailabilityService _availabilityService;

    public MakeupService(
        AppDbContext db,
        MakeupAvailabilityService availabilityService)
    {
        _db = db;
        _availabilityService = availabilityService;
    }

    // =========================================================================
    // Teacher Operations (scoped strictly by TeacherId)
    // =========================================================================

    public async Task<int> GetReviewRequestedCountForTeacherAsync(string teacherId) =>
        await _db.MakeupRequests
            .Where(r => r.TeacherId == teacherId && r.Status == MakeupRequestStatus.ReviewRequested)
            .CountAsync();

    public async Task<int> GetPendingRequestCountForProgramAsync(string programId) =>
        await _db.MakeupRequests
            .Where(r => r.ProgramId == programId && r.Status == MakeupRequestStatus.Pending)
            .CountAsync();

    public async Task<List<MakeupRequestView>> GetTeacherRequestsAsync(string teacherId)
    {
        var requests = await _db.MakeupRequests
            .Where(r => r.TeacherId == teacherId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return await ProjectRequestViewsAsync(requests);
    }

    public async Task<(bool Succeeded, string? Error)> SubmitTeacherRequestAsync(
        string teacherId,
        string assignmentId,
        DateTime missedDate,
        int missedSlotNumber,
        string reason,
        List<PreferredSlot> preferredSlots)
    {
        var assignment = await _db.TeachingAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TeacherId == teacherId);

        if (assignment is null)
        {
            return (false, "Selected course and section assignment was not found for your account.");
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
        {
            return (false, "Please provide a reason (at least 5 characters).");
        }

        // Verify the missed session actually exists on the teacher's regular timetable
        var missedOptions = await _availabilityService.GetMissedSessionOptionsAsync(
            teacherId,
            assignmentId,
            missedDate);

        var matchedSession = missedOptions.FirstOrDefault(o => o.StartSlot == missedSlotNumber);
        if (matchedSession is null)
        {
            return (false, $"You do not have a scheduled class for this course and slot on that date (must be within the last {MakeupAvailabilityService.MaxMissedAgeDays} days, Mon-Fri).");
        }

        var (slotsOk, normalizedSlots, slotError) = await ValidatePreferredSlotsAsync(
            assignment.ProgramId,
            teacherId,
            assignment.SectionId,
            assignment.CourseId,
            matchedSession.DurationHours,
            preferredSlots);

        if (!slotsOk)
        {
            return (false, slotError);
        }

        var now = DateTime.UtcNow;
        var request = new MakeupRequest
        {
            ProgramId = assignment.ProgramId,
            TeacherId = teacherId,
            CourseId = assignment.CourseId,
            SectionId = assignment.SectionId,
            MissedDate = missedDate.Date,
            MissedSlotNumber = matchedSession.StartSlot,
            DurationHours = matchedSession.DurationHours,
            Reason = reason.Trim(),
            PreferredSlots = normalizedSlots,
            Status = MakeupRequestStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.MakeupRequests.Add(request);
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> WithdrawAsync(
        string teacherId,
        string requestId)
    {
        var request = await _db.MakeupRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TeacherId == teacherId);

        if (request is null)
        {
            return (false, "Makeup request was not found for your account.");
        }

        if (request.Status != MakeupRequestStatus.Pending)
        {
            return (false, "Only pending requests can be withdrawn.");
        }

        request.Status = MakeupRequestStatus.Withdrawn;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> ResubmitAsync(
        string teacherId,
        string requestId,
        string reason,
        List<PreferredSlot> preferredSlots)
    {
        var request = await _db.MakeupRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TeacherId == teacherId);

        if (request is null)
        {
            return (false, "Makeup request was not found for your account.");
        }

        if (request.Status != MakeupRequestStatus.ReviewRequested)
        {
            return (false, "Only requests marked for review can be edited and resubmitted.");
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
        {
            return (false, "Please provide a reason (at least 5 characters).");
        }

        var (slotsOk, normalizedSlots, slotError) = await ValidatePreferredSlotsAsync(
            request.ProgramId,
            teacherId,
            request.SectionId,
            request.CourseId,
            request.DurationHours,
            preferredSlots);

        if (!slotsOk)
        {
            return (false, slotError);
        }

        // Keep missed class locked and keep CoordinatorRemarks visible
        request.Reason = reason.Trim();
        request.PreferredSlots = normalizedSlots;
        request.Status = MakeupRequestStatus.Pending;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> MarkHeldAsync(
        string teacherId,
        string requestId,
        string? note)
    {
        var request = await _db.MakeupRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TeacherId == teacherId);

        if (request is null)
        {
            return (false, "Makeup request was not found for your account.");
        }

        if (request.Status is MakeupRequestStatus.Held or MakeupRequestStatus.NotHeld)
        {
            return (false, "Completion outcome has already been recorded and cannot be changed.");
        }

        if (request.Status != MakeupRequestStatus.Approved)
        {
            return (false, "Only approved makeup classes can be marked as held.");
        }

        if (!request.ApprovedDate.HasValue || request.ApprovedDate.Value.Date > DateTime.Today)
        {
            return (false, "You can mark a makeup class as held only on or after its scheduled date.");
        }

        var now = DateTime.UtcNow;
        request.Status = MakeupRequestStatus.Held;
        request.HeldAt = now;
        request.HeldNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        request.UpdatedAt = now;

        // Held makeup entries remain in TimetableEntries as historical record
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> MarkNotHeldAsync(
        string teacherId,
        string requestId,
        string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return (false, "Please provide a note explaining why the makeup class was not held.");
        }

        var request = await _db.MakeupRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TeacherId == teacherId);

        if (request is null)
        {
            return (false, "Makeup request was not found for your account.");
        }

        if (request.Status is MakeupRequestStatus.Held or MakeupRequestStatus.NotHeld)
        {
            return (false, "Completion outcome has already been recorded and cannot be changed.");
        }

        if (request.Status != MakeupRequestStatus.Approved)
        {
            return (false, "Only approved makeup classes can be marked as not held.");
        }

        if (!request.ApprovedDate.HasValue || request.ApprovedDate.Value.Date > DateTime.Today)
        {
            return (false, "You can mark a makeup class as not held only on or after its scheduled date.");
        }

        var now = DateTime.UtcNow;
        request.Status = MakeupRequestStatus.NotHeld;
        request.HeldAt = now;
        request.HeldNote = note.Trim();
        request.UpdatedAt = now;

        await RemoveMakeupEntriesAsync(request.ProgramId, request.Id);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> WithdrawApprovedAsync(
        string teacherId,
        string requestId)
    {
        var request = await _db.MakeupRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TeacherId == teacherId);

        if (request is null)
        {
            return (false, "Makeup request was not found for your account.");
        }

        if (request.Status != MakeupRequestStatus.Approved)
        {
            return (false, "Only approved makeup requests can be withdrawn with this action.");
        }

        if (request.ApprovedDate.HasValue && request.ApprovedDate.Value.Date <= DateTime.Today)
        {
            return (false, "Approved makeup classes on or after their scheduled date cannot be withdrawn; please mark them as Held or Not Held.");
        }

        request.Status = MakeupRequestStatus.Withdrawn;
        request.UpdatedAt = DateTime.UtcNow;

        await RemoveMakeupEntriesAsync(request.ProgramId, request.Id);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task RemoveMakeupEntriesAsync(string programId, string requestId)
    {
        var entries = await _db.TimetableEntries
            .Where(e => e.ProgramId == programId && e.MakeupRequestId == requestId)
            .ToListAsync();

        if (entries.Count > 0)
        {
            _db.TimetableEntries.RemoveRange(entries);
        }
    }

    private async Task<(bool Ok, List<PreferredSlot> Normalized, string? Error)> ValidatePreferredSlotsAsync(
        string programId,
        string teacherId,
        string sectionId,
        string courseId,
        int durationHours,
        List<PreferredSlot> preferredSlots)
    {
        var normalized = (preferredSlots ?? new List<PreferredSlot>())
            .Select(s => new PreferredSlot { Date = s.Date.Date, StartSlot = s.StartSlot })
            .GroupBy(s => (s.Date, s.StartSlot))
            .Select(g => g.First())
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartSlot)
            .ToList();

        if (normalized.Count < 2 || normalized.Count > 4)
        {
            return (false, new List<PreferredSlot>(), "Please select between 2 and 4 preferred start slots.");
        }

        var freeOptions = await _availabilityService.GetFreeStartOptionsAsync(
            programId,
            teacherId,
            sectionId,
            courseId,
            durationHours,
            DateTime.Today);

        var freeKeys = freeOptions
            .Select(o => (o.Date.Date, o.StartSlot))
            .ToHashSet();

        foreach (var slot in normalized)
        {
            if (!freeKeys.Contains((slot.Date.Date, slot.StartSlot)))
            {
                return (false, new List<PreferredSlot>(), "One or more selected preferred slots are not free or valid. Please re-select from the grid.");
            }
        }

        return (true, normalized, null);
    }

    // =========================================================================
    // Coordinator Operations (scoped strictly by ProgramId)
    // =========================================================================

    public async Task<List<MakeupRequestView>> GetProgramRequestsAsync(string programId)
    {
        var requests = await _db.MakeupRequests
            .Where(r => r.ProgramId == programId)
            .ToListAsync();

        var views = await ProjectRequestViewsAsync(requests);
        return views
            .OrderBy(r => r.Status == MakeupRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .ToList();
    }

    public async Task<(bool Succeeded, string? Error)> ApproveRequestAsync(
        string programId,
        string requestId,
        DateTime approvedDate,
        int startSlot,
        string roomId,
        string? remarks)
    {
        var request = await _db.MakeupRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ProgramId == programId);

        if (request is null)
        {
            return (false, "Makeup request was not found for your program.");
        }

        if (request.Status != MakeupRequestStatus.Pending)
        {
            return (false, "Only pending makeup requests can be approved.");
        }

        var targetDate = approvedDate.Date;
        var matchesPreferred = request.PreferredSlots.Any(
            p => p.Date.Date == targetDate && p.StartSlot == startSlot);

        if (!matchesPreferred)
        {
            return (false, "Selected date and start slot must be one of the teacher's preferred slots.");
        }

        var hours = request.DurationHours > 0 ? request.DurationHours : 1;

        // Re-check every hour of the block (teacher, section, room, lab type, capacity, lunch/bounds)
        var (ok, error) = await _availabilityService.CheckBlockAsync(
            programId,
            request.TeacherId,
            request.SectionId,
            roomId,
            targetDate,
            startSlot,
            hours,
            request.CourseId);

        if (!ok)
        {
            return (false, error ?? "Selected slot or room is no longer available.");
        }

        var assignment = await _db.TeachingAssignments
            .FirstOrDefaultAsync(a => a.ProgramId == programId
                                      && a.TeacherId == request.TeacherId
                                      && a.CourseId == request.CourseId
                                      && a.SectionId == request.SectionId);

        var now = DateTime.UtcNow;
        request.Status = MakeupRequestStatus.Approved;
        request.ApprovedRoomId = roomId;
        request.ApprovedDate = targetDate;
        request.ApprovedSlotNumber = startSlot;
        request.ApprovedDurationHours = hours;
        request.CoordinatorRemarks = string.IsNullOrWhiteSpace(remarks)
            ? "Approved and scheduled."
            : remarks.Trim();
        request.ReviewedAt = now;
        request.UpdatedAt = now;

        // Insert one TimetableEntry per hour of the approved makeup block
        var standardSlots = TimetableService.StandardSlots;
        for (int i = 0; i < hours; i++)
        {
            int slotNum = startSlot + i;
            var slotDef = standardSlots[slotNum - 1];
            _db.TimetableEntries.Add(new TimetableEntry
            {
                ProgramId = programId,
                TeachingAssignmentId = assignment?.Id ?? string.Empty,
                TeacherId = request.TeacherId,
                CourseId = request.CourseId,
                SectionId = request.SectionId,
                RoomId = roomId,
                DayOfWeek = targetDate.DayOfWeek,
                SlotNumber = slotNum,
                StartTime = slotDef.StartTime,
                EndTime = slotDef.EndTime,
                IsMakeup = true,
                MakeupDate = targetDate,
                MakeupRequestId = request.Id
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> RejectRequestAsync(
        string programId,
        string requestId,
        string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return (false, "Please provide a reason or note when rejecting a request.");
        }

        var request = await _db.MakeupRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ProgramId == programId);

        if (request is null)
        {
            return (false, "Makeup request was not found for your program.");
        }

        if (request.Status != MakeupRequestStatus.Pending)
        {
            return (false, "Only pending makeup requests can be rejected.");
        }

        var now = DateTime.UtcNow;
        request.Status = MakeupRequestStatus.Rejected;
        request.CoordinatorRemarks = remarks.Trim();
        request.ReviewedAt = now;
        request.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> RequestReviewAsync(
        string programId,
        string requestId,
        string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return (false, "Please provide a note explaining what the teacher should revise.");
        }

        var request = await _db.MakeupRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ProgramId == programId);

        if (request is null)
        {
            return (false, "Makeup request was not found for your program.");
        }

        if (request.Status != MakeupRequestStatus.Pending)
        {
            return (false, "Only pending makeup requests can be sent back for review.");
        }

        var now = DateTime.UtcNow;
        request.Status = MakeupRequestStatus.ReviewRequested;
        request.CoordinatorRemarks = remarks.Trim();
        request.ReviewedAt = now;
        request.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // =========================================================================
    // Shared Projection Helper
    // =========================================================================

    private async Task<List<MakeupRequestView>> ProjectRequestViewsAsync(List<MakeupRequest> requests)
    {
        if (requests.Count == 0)
        {
            return new List<MakeupRequestView>();
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
            Room? approvedRoom = null;
            if (!string.IsNullOrEmpty(r.ApprovedRoomId))
            {
                rooms.TryGetValue(r.ApprovedRoomId, out approvedRoom);
            }

            return new MakeupRequestView(
                Id: r.Id,
                TeacherId: r.TeacherId,
                TeacherName: teacher?.FullName ?? "Unknown Teacher",
                CourseId: r.CourseId,
                CourseCode: course?.Code ?? "N/A",
                CourseTitle: course?.Title ?? "Unknown Course",
                SectionId: r.SectionId,
                SectionName: section?.Name ?? "N/A",
                MissedDate: r.MissedDate,
                MissedSlotNumber: r.MissedSlotNumber,
                Reason: r.Reason,
                Status: r.Status,
                ApprovedDate: r.ApprovedDate,
                ApprovedSlotNumber: r.ApprovedSlotNumber,
                ApprovedRoomCode: approvedRoom?.Code,
                CoordinatorRemarks: r.CoordinatorRemarks,
                CreatedAt: r.CreatedAt,
                DurationHours: r.DurationHours > 0 ? r.DurationHours : 1,
                PreferredSlots: r.PreferredSlots
                    .Select(p => new PreferredSlot { Date = p.Date, StartSlot = p.StartSlot })
                    .OrderBy(p => p.Date)
                    .ThenBy(p => p.StartSlot)
                    .ToList(),
                ApprovedDurationHours: r.ApprovedDurationHours,
                HeldAt: r.HeldAt,
                HeldNote: r.HeldNote,
                UpdatedAt: r.UpdatedAt,
                SectionStudentCount: section?.StudentCount ?? 0);
        }).ToList();
    }
}

public record MakeupRequestView(
    string Id,
    string TeacherId,
    string TeacherName,
    string CourseId,
    string CourseCode,
    string CourseTitle,
    string SectionId,
    string SectionName,
    DateTime MissedDate,
    int MissedSlotNumber,
    string Reason,
    MakeupRequestStatus Status,
    DateTime? ApprovedDate,
    int? ApprovedSlotNumber,
    string? ApprovedRoomCode,
    string? CoordinatorRemarks,
    DateTime CreatedAt,
    int DurationHours = 1,
    List<PreferredSlot>? PreferredSlots = null,
    int? ApprovedDurationHours = null,
    DateTime? HeldAt = null,
    string? HeldNote = null,
    DateTime? UpdatedAt = null,
    int SectionStudentCount = 0);
