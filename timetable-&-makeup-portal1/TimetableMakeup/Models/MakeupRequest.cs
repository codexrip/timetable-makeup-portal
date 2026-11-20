using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Lifecycle status of a makeup class request.
/// </summary>
public enum MakeupRequestStatus
{
    Pending,
    ReviewRequested,
    Approved,
    Rejected,
    Held,
    NotHeld,
    Withdrawn
}

/// <summary>
/// Request submitted by a teacher to reschedule a missed lecture.
/// </summary>
public class MakeupRequest
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ProgramId { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;

    public DateTime MissedDate { get; set; }
    public int MissedSlotNumber { get; set; } // start slot of the missed session
    public int DurationHours { get; set; } = 1; // length of missed session
    public string Reason { get; set; } = string.Empty;

    public List<PreferredSlot> PreferredSlots { get; set; } = new();

    public string? ApprovedRoomId { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public int? ApprovedSlotNumber { get; set; } // start slot of the approved makeup
    public int? ApprovedDurationHours { get; set; } // same as DurationHours when approved

    public MakeupRequestStatus Status { get; set; } = MakeupRequestStatus.Pending;
    public string? CoordinatorRemarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? HeldAt { get; set; }
    public string? HeldNote { get; set; }
}
