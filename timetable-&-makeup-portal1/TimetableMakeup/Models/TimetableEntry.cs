using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Scheduled weekly timetable slot (regular or approved makeup lecture).
/// </summary>
public class TimetableEntry
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ProgramId { get; set; } = string.Empty;
    public string TeachingAssignmentId { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public int SlotNumber { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsMakeup { get; set; }
    public DateTime? MakeupDate { get; set; }
    public string? MakeupRequestId { get; set; }
}
