using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Course offered within an academic program.
/// </summary>
public class Course
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ProgramId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public bool RequiresLab { get; set; }
}
