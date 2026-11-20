using System.Security.Claims;
using TimetableMakeup.Models;

namespace TimetableMakeup.Services.Auth;

/// <summary>
/// Authentication abstraction so hardcoded demo users can be replaced by ASP.NET Identity later.
/// </summary>
public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(string email, string password);
    ClaimsPrincipal CreatePrincipal(AppUser user);
}

public record AuthResult(bool Succeeded, AppUser? User = null, string? ErrorMessage = null);
