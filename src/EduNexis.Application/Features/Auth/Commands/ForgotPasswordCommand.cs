using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Auth.Commands;

public record ForgotPasswordCommand(string Email) : ICommand<ApiResponse>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class ForgotPasswordCommandHandler(
    IUnitOfWork uow,
    IResetTokenGenerator tokenGenerator,
    IEmailService emailService,
    IEmailTemplateBuilder templateBuilder,
    ILogger<ForgotPasswordCommandHandler> logger
) : ICommandHandler<ForgotPasswordCommand, ApiResponse>
{
    private const int TokenExpiryHours = 1;

    public async ValueTask<ApiResponse> Handle(
        ForgotPasswordCommand command, CancellationToken ct)
    {
        // Always return generic success to prevent email enumeration.
        // Whether or not the email exists, the response is identical.
        const string GenericMessage =
            "If an account exists for that email, a password reset link has been sent.";

        var user = await uow.Users.GetByEmailAsync(command.Email, ct);
        if (user is null || !user.IsActive)
        {
            // Log internally but don't tell the caller
            logger.LogInformation(
                "Forgot password requested for non-existent or inactive email: {Email}",
                command.Email);
            return ApiResponse.Ok(GenericMessage);
        }

        // Generate token + persist hash
        var (plainToken, tokenHash) = tokenGenerator.Generate();
        var expiresAt = DateTime.UtcNow.AddHours(TokenExpiryHours);
        var resetToken = PasswordResetToken.Create(user.Id, tokenHash, expiresAt);

        await uow.GetRepository<PasswordResetToken>().AddAsync(resetToken, ct);
        await uow.SaveChangesAsync(ct);

        // Send email — failures swallowed so generic response stays consistent
        try
        {
            var resetUrl = templateBuilder.FrontendBaseUrl
                + "/reset-password?token=" + plainToken;

            var bodyHtml =
                "<p>Hi,</p>" +
                "<p>We received a request to reset your EduNexis password. Click the button below to choose a new password:</p>" +
                templateBuilder.Button(resetUrl, "Reset password") +
                "<p style=\"color:#78716c;font-size:13px;\">" +
                "This link expires in <strong>" + TokenExpiryHours + " hour</strong>. " +
                "If you didn't request a password reset, you can safely ignore this email \u2014 your password will not change." +
                "</p>" +
                "<p style=\"color:#78716c;font-size:12px;\">" +
                "If the button doesn't work, copy and paste this URL into your browser:<br/>" +
                "<span style=\"font-family:monospace;word-break:break-all;\">" + resetUrl + "</span>" +
                "</p>";

            var html = templateBuilder.Build("Reset your password", bodyHtml);
            await emailService.SendAsync(user.Email, "Reset your EduNexis password", html, ct);
            logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send reset email to {Email}", user.Email);
        }

        return ApiResponse.Ok(GenericMessage);
    }
}