using EduNexis.Domain.Common;

namespace EduNexis.Application.Features.Auth.Commands;

public record LogoutUserCommand(Guid UserId) : ICommand<ApiResponse>;

public sealed class LogoutUserCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<LogoutUserCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        LogoutUserCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(command.UserId, ct);
        if (user is null)
            return ApiResponse.Fail("User not found.");

        user.ClearRefreshToken();
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Logged out successfully.");
    }
}
