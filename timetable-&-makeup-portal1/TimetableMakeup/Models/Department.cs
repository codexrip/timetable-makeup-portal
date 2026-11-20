using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Top-level academic department (e.g., Department of Information Technology).
/// </summary>
public class Department
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
