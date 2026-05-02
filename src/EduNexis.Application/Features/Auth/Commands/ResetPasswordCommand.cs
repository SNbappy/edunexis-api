using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommand<ApiResponse>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MinimumLength(20);
        RuleFor(x => x.NewPassword)
            .NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Must contain at least one number.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain at least one special character.");
    }
}

public sealed class ResetPasswordCommandHandler(
    IUnitOfWork uow,
    IPasswordHasher passwordHasher
) : ICommandHandler<ResetPasswordCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        ResetPasswordCommand command, CancellationToken ct)
    {
        // Find unused, unexpired tokens (we have to scan since we hash) — limit to last 50 to keep it bounded
        var candidates = await uow.GetRepository<PasswordResetToken>()
            .FindAsync(t => t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow, ct);

        // Find which one matches by hash compare
        PasswordResetToken? matched = null;
        foreach (var t in candidates)
        {
            if (passwordHasher.Verify(command.Token, t.TokenHash))
            {
                matched = t;
                break;
            }
        }

        if (matched is null)
            return ApiResponse.Fail("This reset link is invalid or has expired. Please request a new one.");

        if (!matched.IsValid())
            return ApiResponse.Fail("This reset link is invalid or has expired. Please request a new one.");

        var user = await uow.Users.GetByIdAsync(matched.UserId, ct);
        if (user is null || !user.IsActive)
            return ApiResponse.Fail("This reset link is invalid or has expired. Please request a new one.");

        // Update password
        var newHash = passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newHash);

        // Invalidate all existing sessions
        user.ClearRefreshToken();

        // Mark token used (single-use)
        matched.MarkUsed();

        uow.Users.Update(user);
        uow.GetRepository<PasswordResetToken>().Update(matched);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Your password has been reset. Please sign in with your new password.");
    }
}