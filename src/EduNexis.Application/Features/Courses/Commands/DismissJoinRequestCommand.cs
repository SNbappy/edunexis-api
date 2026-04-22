using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Courses.Commands;

public record DismissJoinRequestCommand(Guid RequestId) : ICommand<ApiResponse>;

public sealed class DismissJoinRequestCommandValidator : AbstractValidator<DismissJoinRequestCommand>
{
    public DismissJoinRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class DismissJoinRequestCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<DismissJoinRequestCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DismissJoinRequestCommand cmd, CancellationToken ct)
    {
        var userId = Guid.Parse(currentUser.UserId);

        var request = await uow.JoinRequests.GetByIdAsync(cmd.RequestId, ct);
        if (request is null)
            return ApiResponse.Fail("Join request not found.");

        if (request.StudentId != userId)
            return ApiResponse.Fail("You can only dismiss your own join requests.");

        try
        {
            request.DismissByStudent();
        }
        catch (DomainException ex)
        {
            return ApiResponse.Fail(ex.Message);
        }

        uow.JoinRequests.Update(request);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Request dismissed.");
    }
}
