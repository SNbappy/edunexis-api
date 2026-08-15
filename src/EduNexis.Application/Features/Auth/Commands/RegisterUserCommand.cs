using EduNexis.Application.DTOs;
using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Auth.Commands;

public record RegisterUserCommand(string Email, string Password, string FullName) : ICommand<ApiResponse<AuthResponseDto>>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .Must(e => e.EndsWith("@just.edu.bd", StringComparison.OrdinalIgnoreCase) ||
                       e.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only @just.edu.bd or @student.just.edu.bd emails are allowed.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Must contain at least one number.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain at least one special character.");
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2);
    }
}

public sealed class RegisterUserCommandHandler(
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    IOtpGenerator otpGenerator,
    IEmailService emailService,
    IEmailTemplateBuilder templateBuilder,
    ILogger<RegisterUserCommandHandler> logger
) : ICommandHandler<RegisterUserCommand, ApiResponse<AuthResponseDto>>
{
    private const int OtpExpiryMinutes = 10;

    public async ValueTask<ApiResponse<AuthResponseDto>> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        var existing = await uow.Users.GetByEmailAsync(command.Email, ct);
        if (existing is not null)
            return ApiResponse<AuthResponseDto>.Fail("Email already registered.");

        var role = command.Email.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase)
            ? UserRole.Student : UserRole.Teacher;

        var user = User.Create(command.Email, passwordHasher.Hash(command.Password), role);
        await uow.Users.AddAsync(user, ct);

        var profile = UserProfile.Create(user.Id, command.FullName);
        await uow.UserProfiles.AddAsync(profile, ct);

        // Generate OTP, hash, persist on user
        var (plainOtp, otpHash) = otpGenerator.Generate();
        var expiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);
        user.SetEmailOtp(otpHash, expiresAt);

        await uow.SaveChangesAsync(ct);

        // Send OTP email (errors swallowed inside EmailService, but log here for clarity)
        await SendOtpEmailAsync(user.Email, plainOtp, ct);

        // Return WITHOUT auth tokens — user must verify first
        return ApiResponse<AuthResponseDto>.Ok(
            new AuthResponseDto(
                AccessToken: string.Empty,
                RefreshToken: string.Empty,
                ExpiresIn: 0,
                User: null,
                VerificationRequired: true,
                PendingEmail: user.Email),
            "Registration successful. Please check your email for a verification code.");
    }

    private async Task SendOtpEmailAsync(string email, string otp, CancellationToken ct)
    {
        try
        {
            var bodyHtml =
                "<p>Welcome to EduNexis!</p>" +
                "<p>Your verification code is:</p>" +
                $"<div style=\"font-size:32px;font-weight:700;letter-spacing:8px;color:#0d9488;background:#f0fdfa;padding:16px 24px;border-radius:12px;text-align:center;margin:24px 0;border:1px solid #99f6e4;\">{otp}</div>" +
                $"<p>This code will expire in <strong>{OtpExpiryMinutes} minutes</strong>.</p>" +
                "<p style=\"color:#78716c;font-size:13px;\">If you didn't request this, please ignore this email — someone may have entered your email by mistake.</p>";

            var html = templateBuilder.Build("Verify your email", bodyHtml);

            // The subject deliberately does NOT carry the code. It used to read
            // "EduNexis verification code: 123456", which put a one-time secret into
            // lock-screen notification previews and inbox list rows, where it can be
            // read without unlocking the device or even opening the message.
            var sent = await emailService.SendAsync(email, "Verify your EduNexis email", html, ct);

            if (sent)
                logger.LogInformation("OTP email sent to {Email}", email);
            else
                logger.LogError(
                    "OTP email to {Email} was NOT delivered - the provider rejected it. "
                    + "This account cannot be verified until email delivery is fixed.",
                    email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP email to {Email}", email);
        }
    }
}