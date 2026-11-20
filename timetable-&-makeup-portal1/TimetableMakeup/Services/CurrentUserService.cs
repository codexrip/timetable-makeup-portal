using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TimetableMakeup.Models;

namespace TimetableMakeup.Services;

/// <summary>
/// Exposes the authenticated user's Role, ProgramId, and TeacherId strictly from cookie claims.
/// </summary>
public class CurrentUserService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        AuthenticationStateProvider authStateProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _authStateProvider = authStateProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ClaimsPrincipal?> GetPrincipalAsync()
    {
        try
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            return state.User;
        }
        catch
        {
            return _httpContextAccessor.HttpContext?.User;
        }
    }

    public async Task<CurrentUserInfo> GetCurrentUserAsync()
    {
        var user = await GetPrincipalAsync();
        if (user?.Identity?.IsAuthenticated != true)
        {
            return new CurrentUserInfo(false, string.Empty, string.Empty, null, null, null);
        }

        var roleText = user.FindFirstValue(ClaimTypes.Role);
        Role? parsedRole = Enum.TryParse<Role>(roleText, out var role) ? role : null;

        return new CurrentUserInfo(
            IsAuthenticated: true,
            FullName: user.FindFirstValue(ClaimTypes.Name) ?? "User",
            Email: user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Role: parsedRole,
            ProgramId: user.FindFirstValue("ProgramId"),
            TeacherId: user.FindFirstValue("TeacherId"));
    }

    public async Task<Role?> GetRoleAsync() => (await GetCurrentUserAsync()).Role;
    public async Task<string?> GetProgramIdAsync() => (await GetCurrentUserAsync()).ProgramId;
    public async Task<string?> GetTeacherIdAsync() => (await GetCurrentUserAsync()).TeacherId;
    public async Task<string> GetFullNameAsync() => (await GetCurrentUserAsync()).FullName;
}

public record CurrentUserInfo(
    bool IsAuthenticated,
    string FullName,
    string Email,
    Role? Role,
    string? ProgramId,
    string? TeacherId);
