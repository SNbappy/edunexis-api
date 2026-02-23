using EduNexis.Application.DTOs;
using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Auth.Commands;

public record LoginUserCommand(
    string Email,
    string Password
) : ICommand<ApiResponse<AuthResponseDto>>;

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginUserCommandHandler(
    IUnitOfWork uow,
    IJwtTokenService jwtService,
    IPasswordHasher passwordHasher
) : ICommandHandler<LoginUserCommand, ApiResponse<AuthResponseDto>>
{
    public async ValueTask<ApiResponse<AuthResponseDto>> Handle(
        LoginUserCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByEmailAsync(command.Email, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = jwtService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await uow.SaveChangesAsync(ct);

        var profile = await uow.UserProfiles.GetByUserIdAsync(user.Id, ct);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: 3600,
            User: new UserDto(
                user.Id, user.Email, user.Role.ToString(),
                user.IsProfileComplete,
                profile is null ? null : new UserProfileDto(
                    profile.Id, profile.FullName, profile.Department,
                    profile.Designation, profile.StudentId, profile.Bio,
                    profile.ProfilePhotoUrl, profile.PhoneNumber,
                    profile.LinkedInUrl, profile.ProfileCompletionPercent))),
            "Login successful.");
    }
}
