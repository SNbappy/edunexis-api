using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Profile.Commands;
using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Auth.Commands;

public record VerifyEmailOtpCommand(string Email, string Otp) : ICommand<ApiResponse<AuthResponseDto>>;

public sealed class VerifyEmailOtpCommandValidator : AbstractValidator<VerifyEmailOtpCommand>
{
    public VerifyEmailOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Otp).NotEmpty().Length(6).Matches(@"^\d{6}$")
            .WithMessage("OTP must be a 6-digit number.");
    }
}

public sealed class VerifyEmailOtpCommandHandler(
    IUnitOfWork uow,
    IJwtTokenService jwtService,
    IPasswordHasher passwordHasher
) : ICommandHandler<VerifyEmailOtpCommand, ApiResponse<AuthResponseDto>>
{
    public async ValueTask<ApiResponse<AuthResponseDto>> Handle(
        VerifyEmailOtpCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByEmailAsync(command.Email, ct);
        if (user is null)
            return ApiResponse<AuthResponseDto>.Fail("Account not found.");

        if (user.IsEmailVerified)
            return ApiResponse<AuthResponseDto>.Fail("Email is already verified. Please log in.");

        if (!user.TryConsumeOtp(command.Otp, passwordHasher))
            return ApiResponse<AuthResponseDto>.Fail("The code is invalid or has expired. Please request a new code.");

        // Issue tokens — verification = login
        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = jwtService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await uow.SaveChangesAsync(ct);

        var profile = await uow.UserProfiles.GetByUserIdAsync(user.Id, ct);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: 3600,
            User: new UserDto(user.Id, user.Email, user.Role.ToString(), user.IsProfileComplete,
                profile is null ? null : UpdateProfileCommandHandler.MapToDto(profile))),
            "Email verified successfully.");
    }
}