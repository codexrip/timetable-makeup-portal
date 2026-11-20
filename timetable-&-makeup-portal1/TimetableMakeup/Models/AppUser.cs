using MongoDB.Bson;

namespace TimetableMakeup.Models;

/// <summary>
/// Extensible portal roles (Admin and HoD reserved for future phases).
/// </summary>
public enum Role
{
    Admin,
    HoD,
    Coordinator,
    Teacher
}

/// <summary>
/// Application user account for authentication and role/program scoping.
/// </summary>
public class AppUser
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Role Role { get; set; }
    public string? ProgramId { get; set; }
    public string? TeacherId { get; set; }
}
