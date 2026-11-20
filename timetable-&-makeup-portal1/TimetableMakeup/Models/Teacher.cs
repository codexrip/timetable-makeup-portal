using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Faculty member assigned to an academic program.
/// </summary>
public class Teacher
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ProgramId { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
}
