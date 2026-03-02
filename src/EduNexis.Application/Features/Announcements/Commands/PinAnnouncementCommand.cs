namespace EduNexis.Application.Features.Announcements.Commands;

public record PinAnnouncementCommand(Guid CourseId, Guid AnnouncementId)
    : ICommand<ApiResponse>;

public sealed class PinAnnouncementCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<PinAnnouncementCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        PinAnnouncementCommand cmd, CancellationToken ct)
    {
        var requesterId = Guid.Parse(currentUser.UserId);

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        if (course is null)
            return ApiResponse.Fail("Course not found.");

        if (course.TeacherId != requesterId && currentUser.Role != "Admin")
            return ApiResponse.Fail("Only the teacher can pin announcements.");

        var announcement = await uow.GetRepository<Announcement>()
            .GetByIdAsync(cmd.AnnouncementId, ct);

        if (announcement is null || announcement.IsDeleted || announcement.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Announcement not found.");

        if (announcement.IsPinned) announcement.Unpin();
        else announcement.Pin();

        uow.GetRepository<Announcement>().Update(announcement);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok(announcement.IsPinned ? "Announcement pinned." : "Announcement unpinned.");
    }
}
