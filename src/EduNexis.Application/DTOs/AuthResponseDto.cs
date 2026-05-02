namespace EduNexis.Application.DTOs;

/// <summary>
/// Auth response — issued on successful login or after OTP verification.
/// When VerificationRequired is true, AccessToken and RefreshToken are empty
/// and the client should redirect to /verify-email.
/// </summary>
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserDto? User,
    bool VerificationRequired = false,
    string? PendingEmail = null);