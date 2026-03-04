using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Profile.Commands;
using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : ICommand<ApiResponse<AuthResponseDto>>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler(
    IUnitOfWork uow, IJwtTokenService jwtService
) : ICommandHandler<RefreshTokenCommand, ApiResponse<AuthResponseDto>>
{
    public async ValueTask<ApiResponse<AuthResponseDto>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByRefreshTokenAsync(command.RefreshToken, ct);
        if (user is null || !user.IsRefreshTokenValid(command.RefreshToken))
            return ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token.");

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = jwtService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await uow.SaveChangesAsync(ct);

        var profile = await uow.UserProfiles.GetByUserIdAsync(user.Id, ct);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto(
            AccessToken: accessToken, RefreshToken: refreshToken, ExpiresIn: 3600,
            User: new UserDto(user.Id, user.Email, user.Role.ToString(), user.IsProfileComplete,
                profile is null ? null : UpdateProfileCommandHandler.MapToDto(profile))),
            "Token refreshed.");
    }
}
