using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.CT.Commands;

public record PublishCTCommand(
    Guid CTEventId,
    Guid TeacherId
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var e = await uow.GetRepository<CTEvent>().GetByIdAsync(CTEventId, ct);
        return e?.CourseId;
    }
}

public sealed class PublishCTCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<PublishCTCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        PublishCTCommand command, CancellationToken ct)
    {
        var ctEvent = await uow.GetRepository<CTEvent>().GetByIdAsync(command.CTEventId, ct)
            ?? throw new NotFoundException("CTEvent", command.CTEventId);

        var course = await uow.Courses.GetByIdAsync(ctEvent.CourseId, ct)
            ?? throw new NotFoundException("Course", ctEvent.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            return ApiResponse.Fail("Only the teacher can publish CT results.");

        if (!ctEvent.KhataUploaded)
            return ApiResponse.Fail("All 3 khata must be uploaded before publishing.");

        ctEvent.Publish();
        uow.GetRepository<CTEvent>().Update(ctEvent);
        await uow.SaveChangesAsync(ct);

        // Notify all active students that results are published
        var members = await uow.CourseMembers.GetByCourseAsync(ctEvent.CourseId, ct);
        foreach (var m in members.Where(x => x.IsActive))
        {
            await sender.Send(new SendNotificationCommand(
                UserId: m.UserId,
                Title: $"CT results published in {course.Title}",
                Body: $"CT {ctEvent.CTNumber}: \"{ctEvent.Title}\" — your marks are now available.",
                Type: NotificationType.General,
                RedirectUrl: $"/courses/{course.Id}/ct"
            ), ct);
        }

        return ApiResponse.Ok("CT published. Students can now view their marks.");
    }
}