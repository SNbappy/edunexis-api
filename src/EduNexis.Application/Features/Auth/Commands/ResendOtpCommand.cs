using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Auth.Commands;

public record ResendOtpCommand(string Email) : ICommand<ApiResponse>;

public sealed class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class ResendOtpCommandHandler(
    IUnitOfWork uow,
    IOtpGenerator otpGenerator,
    IEmailService emailService,
    IEmailTemplateBuilder templateBuilder,
    ILogger<ResendOtpCommandHandler> logger
) : ICommandHandler<ResendOtpCommand, ApiResponse>
{
    private const int OtpExpiryMinutes = 10;
    private const int CooldownSeconds = 60;

    public async ValueTask<ApiResponse> Handle(ResendOtpCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByEmailAsync(command.Email, ct);

        // Don't reveal whether the email exists — return generic success
        if (user is null || user.IsEmailVerified)
        {
            return ApiResponse.Ok("If an unverified account exists for that email, a new code has been sent.");
        }

        if (!user.CanResendOtp(CooldownSeconds))
        {
            var wait = user.OtpResendWaitSeconds(CooldownSeconds);
            return ApiResponse.Fail($"Please wait {wait} seconds before requesting another code.");
        }

        var (plainOtp, otpHash) = otpGenerator.Generate();
        user.SetEmailOtp(otpHash, DateTime.UtcNow.AddMinutes(OtpExpiryMinutes));
        await uow.SaveChangesAsync(ct);

        try
        {
            var bodyHtml =
                "<p>Your new EduNexis verification code is:</p>" +
                $"<div style=\"font-size:32px;font-weight:700;letter-spacing:8px;color:#0d9488;background:#f0fdfa;padding:16px 24px;border-radius:12px;text-align:center;margin:24px 0;border:1px solid #99f6e4;\">{plainOtp}</div>" +
                $"<p>This code will expire in <strong>{OtpExpiryMinutes} minutes</strong>.</p>" +
                "<p style=\"color:#78716c;font-size:13px;\">Previous codes are no longer valid.</p>";

            var html = templateBuilder.Build("New verification code", bodyHtml);
            // Code stays out of the subject - see RegisterUserCommand for why.
            var sent = await emailService.SendAsync(user.Email, "Your new EduNexis verification code", html, ct);

            if (sent)
                logger.LogInformation("OTP resent to {Email}", user.Email);
            else
                logger.LogError(
                    "Resent OTP to {Email} was NOT delivered - the provider rejected it.",
                    user.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend OTP to {Email}", user.Email);
        }

        return ApiResponse.Ok("A new verification code has been sent to your email.");
    }
}