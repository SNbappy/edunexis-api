using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Application.Abstractions;
using EduNexis.Domain.Interfaces.Services;
namespace EduNexis.Application.Features.Courses.Commands;

public record LeaveCourseCommand(Guid CourseId, string Password) : ICommand<ApiResponse>, IArchiveExempt
{
    public string ArchiveExemptionReason => "A student may always leave, including an archived course.";
}

public sealed class LeaveCourseCommandValidator : AbstractValidator<LeaveCourseCommand>
{
    public LeaveCourseCommandValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password confirmation is required to leave a course.");
    }
}

public sealed class LeaveCourseCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher,
    ISender sender
) : ICommandHandler<LeaveCourseCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        LeaveCourseCommand cmd, CancellationToken ct)
    {
        var userId = Guid.Parse(currentUser.UserId);

        // Verify the student's password before making any change.
        var user = await uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (!passwordHasher.Verify(cmd.Password, user.PasswordHash))
            return ApiResponse.Fail("Incorrect password. You have not been removed from the course.");

        var member = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, userId, ct);
        if (member is null || !member.IsActive)
            return ApiResponse.Fail("You are not enrolled in this course.");

        member.Remove();
        uow.CourseMembers.Update(member);
        await uow.SaveChangesAsync(ct);

        // Tell the teacher. A roster that silently shrinks is how a student
        // ends up missing from a gradebook with nobody knowing why.
        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is not null)
        {
            var profile = await uow.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            await sender.Send(new SendNotificationCommand(
                UserId: course.TeacherId,
                Title: $"A student left {course.Title}",
                Body: $"{profile?.FullName ?? "A student"} is no longer enrolled.",
                Type: NotificationType.MemberLeft,
                RedirectUrl: $"/courses/{course.Id}/members"
            ), ct);
        }

        return ApiResponse.Ok("Left the course successfully.");
    }
}
