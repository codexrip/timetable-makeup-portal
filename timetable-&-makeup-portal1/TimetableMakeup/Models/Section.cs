using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Student class section belonging to an academic program.
/// </summary>
public class Section
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ProgramId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Semester { get; set; }
    public string Shift { get; set; } = "Morning";
    public int StudentCount { get; set; }
}
