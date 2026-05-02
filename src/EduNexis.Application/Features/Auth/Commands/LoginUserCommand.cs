using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Profile.Commands;
using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Auth.Commands;

public record LoginUserCommand(string Email, string Password) : ICommand<ApiResponse<AuthResponseDto>>;

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
    IPasswordHasher passwordHasher,
    IOtpGenerator otpGenerator,
    IEmailService emailService,
    IEmailTemplateBuilder templateBuilder,
    IAuthSettings authSettings,
    ILogger<LoginUserCommandHandler> logger
) : ICommandHandler<LoginUserCommand, ApiResponse<AuthResponseDto>>
{
    private const int OtpExpiryMinutes = 10;

    public async ValueTask<ApiResponse<AuthResponseDto>> Handle(LoginUserCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByEmailAsync(command.Email, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        // Block unverified users when OTP is required
        if (authSettings.OtpRequired && !user.IsEmailVerified)
        {
            // Auto-issue a fresh OTP if none active or expired
            if (string.IsNullOrEmpty(user.EmailVerificationOtpHash) ||
                user.EmailVerificationOtpExpiresAt is null ||
                user.EmailVerificationOtpExpiresAt <= DateTime.UtcNow)
            {
                var (plainOtp, otpHash) = otpGenerator.Generate();
                user.SetEmailOtp(otpHash, DateTime.UtcNow.AddMinutes(OtpExpiryMinutes));
                await uow.SaveChangesAsync(ct);
                await SendOtpEmailAsync(user.Email, plainOtp, ct);
            }

            return ApiResponse<AuthResponseDto>.Ok(
                new AuthResponseDto(
                    AccessToken: string.Empty,
                    RefreshToken: string.Empty,
                    ExpiresIn: 0,
                    User: null,
                    VerificationRequired: true,
                    PendingEmail: user.Email),
                "Email not verified. We've sent you a verification code.");
        }

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = jwtService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await uow.SaveChangesAsync(ct);

        var profile = await uow.UserProfiles.GetByUserIdAsync(user.Id, ct);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto(
            AccessToken: accessToken, RefreshToken: refreshToken, ExpiresIn: 3600,
            User: new UserDto(user.Id, user.Email, user.Role.ToString(), user.IsProfileComplete,
                profile is null ? null : UpdateProfileCommandHandler.MapToDto(profile))),
            "Login successful.");
    }

    private async Task SendOtpEmailAsync(string email, string otp, CancellationToken ct)
    {
        try
        {
            var bodyHtml =
                "<p>Your EduNexis verification code is:</p>" +
                $"<div style=\"font-size:32px;font-weight:700;letter-spacing:8px;color:#0d9488;background:#f0fdfa;padding:16px 24px;border-radius:12px;text-align:center;margin:24px 0;border:1px solid #99f6e4;\">{otp}</div>" +
                $"<p>This code will expire in <strong>{OtpExpiryMinutes} minutes</strong>.</p>";

            var html = templateBuilder.Build("Verify your email", bodyHtml);
            await emailService.SendAsync(email, "EduNexis verification code: " + otp, html, ct);
            logger.LogInformation("OTP email re-sent on login to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to re-send OTP email on login to {Email}", email);
        }
    }
}