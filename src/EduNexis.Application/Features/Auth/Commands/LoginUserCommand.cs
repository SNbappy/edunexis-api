using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Profile.Commands;
using EduNexis.Domain.Enums;
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

    /// <summary>
    /// Student addresses can never be promoted, whatever the configuration says.
    /// A student account holding SuperAdmin is the exact failure this guard
    /// exists to prevent, and a typo in an env var should not be able to cause
    /// it. The domain layer already refuses to *create* a Student on any other
    /// domain (User.ValidateEmailMatchesRole), so this closes the same rule on
    /// the promotion path.
    /// </summary>
    private const string StudentEmailDomain = "@student.just.edu.bd";

    public async ValueTask<ApiResponse<AuthResponseDto>> Handle(LoginUserCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByEmailAsync(command.Email, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        // Admin bootstrap: emails listed in Auth:AdminEmails are promoted to
        // SuperAdmin on login. Idempotent — only writes when the role actually
        // needs to change, and the list is empty unless a deployment sets it.
        if (authSettings.AdminEmails.Contains(user.Email) && user.Role != UserRole.SuperAdmin)
        {
            if (user.Email.EndsWith(StudentEmailDomain, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Refusing to promote {Email} to SuperAdmin: student accounts are never " +
                    "eligible. Remove it from Auth:AdminEmails and use a dedicated admin address.",
                    user.Email);
            }
            else
            {
                user.SetRole(UserRole.SuperAdmin);
                await uow.SaveChangesAsync(ct);
                logger.LogInformation("Promoted {Email} to SuperAdmin on login.", user.Email);
            }
        }

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
            // Code stays out of the subject - see RegisterUserCommand for why.
            var sent = await emailService.SendAsync(email, "Verify your EduNexis email", html, ct);

            if (sent)
                logger.LogInformation("OTP email re-sent on login to {Email}", email);
            else
                logger.LogError(
                    "Login OTP to {Email} was NOT delivered - the provider rejected it.",
                    email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to re-send OTP email on login to {Email}", email);
        }
    }
}