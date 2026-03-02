namespace EduNexis.Application.Features.Announcements.Commands;

public record DeleteAnnouncementCommand(Guid CourseId, Guid AnnouncementId)
    : ICommand<ApiResponse>;

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

        bool isTeacher = course.TeacherId == requesterId;
        bool isAdmin   = currentUser.Role == "Admin";
        bool isAuthor  = announcement.AuthorId == requesterId;

        if (!isTeacher && !isAdmin && !isAuthor)
            return ApiResponse.Fail("You are not authorized to delete this announcement.");

        announcement.Delete();
        uow.GetRepository<Announcement>().Update(announcement);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Announcement deleted.");
    }
}
