namespace EduNexis.Application.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserDto User);
