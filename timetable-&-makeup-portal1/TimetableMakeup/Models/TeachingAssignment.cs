using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Links a Teacher, Course, and Section within a Program for a semester.
/// </summary>
public class TeachingAssignment
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ProgramId { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string CourseId { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string SemesterTerm { get; set; } = string.Empty;
}
