using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Marks.Commands;

public record PublishFinalMarksCommand(
    Guid CourseId,
    Guid TeacherId
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class PublishFinalMarksCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<PublishFinalMarksCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        PublishFinalMarksCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (course.TeacherId != command.TeacherId)
            throw new UnauthorizedException("Only the teacher can publish marks.");

        var finalMarks = await uow.GetRepository<FinalMark>()
            .FindAsync(fm => fm.CourseId == command.CourseId, ct);

        var markList = finalMarks.ToList();
        if (markList.Count == 0)
            return ApiResponse.Fail("No final marks found. Please calculate first.");

        foreach (var mark in markList)
            mark.Publish();

        await uow.SaveChangesAsync(ct);

        foreach (var mark in markList)
        {
            await sender.Send(new SendNotificationCommand(
                UserId: mark.StudentId,
                Title: $"Final Marks Published — {course.Title}",
                Body: $"Your final mark is {mark.FinalMarkValue:F2}. Check your result now.",
                Type: NotificationType.MarksPublished,
                RedirectUrl: $"/courses/{course.Id}/marks"
            ), ct);
        }

        return ApiResponse.Ok($"Final marks published for {markList.Count} student(s).");
    }
}
