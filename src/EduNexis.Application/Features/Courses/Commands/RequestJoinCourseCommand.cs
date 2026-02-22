namespace EduNexis.Application.Features.Courses.Commands;

public record RequestJoinCourseCommand(
    Guid StudentId,
    string JoiningCode
) : ICommand<ApiResponse>;

public sealed class RequestJoinCourseCommandValidator : AbstractValidator<RequestJoinCourseCommand>
{
    public RequestJoinCourseCommandValidator()
    {
        RuleFor(x => x.JoiningCode).NotEmpty().Length(8);
    }
}

public sealed class RequestJoinCourseCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<RequestJoinCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        RequestJoinCourseCommand command, CancellationToken ct)
    {
        // 1. Check student profile
        var student = await uow.Users.GetWithProfileAsync(command.StudentId, ct)
            ?? throw new NotFoundException("User", command.StudentId);

        if (!student.IsProfileComplete)
            throw new ProfileIncompleteException();

        // 2. Find course by joining code
        var course = await uow.Courses.GetByJoiningCodeAsync(command.JoiningCode, ct)
            ?? throw new NotFoundException("Course", command.JoiningCode);

        if (course.IsArchived)
            return ApiResponse.Fail("This course is archived and not accepting new members.");

        // 3. Check not already a member
        var existing = await uow.CourseMembers.GetMemberAsync(course.Id, command.StudentId, ct);
        if (existing is not null)
            throw new AlreadyMemberException();

        // 4. Check no duplicate pending request
        var pending = await uow.JoinRequests.GetPendingAsync(course.Id, command.StudentId, ct);
        if (pending is not null)
            throw new DuplicateJoinRequestException();

        // 5. Create join request
        var request = JoinRequest.Create(course.Id, command.StudentId);
        await uow.JoinRequests.AddAsync(request, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Join request submitted successfully.");
    }
}
