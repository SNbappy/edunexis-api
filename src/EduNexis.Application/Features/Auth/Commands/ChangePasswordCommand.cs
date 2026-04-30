using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Auth.Commands;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
) : ICommand<ApiResponse<bool>>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Must contain at least one number.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain at least one special character.");

        RuleFor(x => x)
            .Must(x => x.NewPassword != x.CurrentPassword)
            .WithMessage("New password must be different from current password.")
            .When(x => !string.IsNullOrEmpty(x.CurrentPassword) && !string.IsNullOrEmpty(x.NewPassword));
    }
}

public sealed class ChangePasswordCommandHandler(
    IUnitOfWork uow,
    IPasswordHasher passwordHasher
) : ICommandHandler<ChangePasswordCommand, ApiResponse<bool>>
{
    public async ValueTask<ApiResponse<bool>> Handle(
        ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("User", command.UserId);

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            return ApiResponse<bool>.Fail("Current password is incorrect.");

        var newHash = passwordHasher.Hash(command.NewPassword);
        user.ChangePassword(newHash);

        // Force re-login on OTHER sessions: clear refresh token, then issue a fresh one.
        // The current session keeps working until next refresh; client should immediately
        // call /refresh after success to get a new pair.
        user.ClearRefreshToken();

        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true, "Password changed successfully.");
    }
}