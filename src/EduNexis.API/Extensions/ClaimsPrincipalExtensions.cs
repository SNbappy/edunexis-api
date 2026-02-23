using System.Security.Claims;

namespace EduNexis.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("db_user_id")
            ?? user.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("User ID not found in token.");

        return Guid.Parse(claim.Value);
    }

    public static string GetFirebaseUid(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedException("Firebase UID not found in token.");
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? throw new UnauthorizedException("Email not found in token.");
    }
}
