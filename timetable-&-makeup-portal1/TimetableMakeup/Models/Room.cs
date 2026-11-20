using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Classroom or lab allocated to an academic program.
/// </summary>
public class Room
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ProgramId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsLab { get; set; }
    public int Capacity { get; set; }
}
