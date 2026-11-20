using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TimetableMakeup.Data;
using TimetableMakeup.Models;

namespace TimetableMakeup.Services.Auth;

/// <summary>
/// Authenticates users against the seeded demo accounts stored in MongoDB.
/// </summary>
public class HardcodedAuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ILogger<HardcodedAuthService> _logger;

    public HardcodedAuthService(AppDbContext db, ILogger<HardcodedAuthService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return new AuthResult(false, ErrorMessage: "Email and password are required.");
        }

        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _db.AppUsers
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Password == password);

            if (user is null)
            {
                return new AuthResult(false, ErrorMessage: "Invalid email or password.");
            }

            return new AuthResult(true, User: user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while authenticating user {Email}.", email);
            return new AuthResult(false, ErrorMessage: "Unable to connect to the database. Please ensure MongoDB is running.");
        }
    }

    public ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.ProgramId))
        {
            claims.Add(new Claim("ProgramId", user.ProgramId));
        }

        if (!string.IsNullOrWhiteSpace(user.TeacherId))
        {
            claims.Add(new Claim("TeacherId", user.TeacherId));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
