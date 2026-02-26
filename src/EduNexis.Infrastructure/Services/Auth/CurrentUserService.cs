using EduNexis.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EduNexis.Infrastructure.Services.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub")
        ?? string.Empty;

    public string Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email")
        ?? string.Empty;

    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role)
        ?? string.Empty;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;
}
