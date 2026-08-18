using EduNexis.Application.Abstractions;
namespace EduNexis.Application.Features.Announcements.Commands;

public record DeleteAnnouncementCommand(Guid CourseId, Guid AnnouncementId)
    : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class DeleteAnnouncementCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<DeleteAnnouncementCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteAnnouncementCommand cmd, CancellationToken ct)
    {
        var requesterId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        var announcement = await uow.GetRepository<Announcement>()
            .GetByIdAsync(cmd.AnnouncementId, ct);

        if (announcement is null || announcement.IsDeleted || announcement.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Announcement not found.");

        bool isTeacher = await CourseAccess.IsTeacherAsync(uow, course, requesterId, ct);
        bool isAdmin   = currentUser.Role is "SuperAdmin";
        bool isAuthor  = announcement.AuthorId == requesterId;

        if (!isTeacher && !isAdmin && !isAuthor)
            return ApiResponse.Fail("You are not authorized to delete this announcement.");

        announcement.Delete();
        uow.GetRepository<Announcement>().Update(announcement);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Announcement deleted.");
    }
}
