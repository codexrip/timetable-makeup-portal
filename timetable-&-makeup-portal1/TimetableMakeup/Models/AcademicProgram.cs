using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Academic degree program belonging to a Department (e.g., BSIT).
/// </summary>
public class AcademicProgram
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string DepartmentId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
