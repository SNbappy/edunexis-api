using EduNexis.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;
using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Presentations.Commands;

public record PublishPresentationCommand(
    Guid PresentationEventId,
    Guid TeacherId
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var e = await uow.GetRepository<PresentationEvent>().GetByIdAsync(PresentationEventId, ct);
        return e?.CourseId;
    }
}

public sealed class PublishPresentationCommandHandler(
    IUnitOfWork uow,
    ISender sender,
    ILogger<PublishPresentationCommandHandler> logger
) : ICommandHandler<PublishPresentationCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        PublishPresentationCommand command, CancellationToken ct)
    {
        var presentation = await uow.GetRepository<PresentationEvent>()
            .GetByIdAsync(command.PresentationEventId, ct)
            ?? throw new NotFoundException("PresentationEvent", command.PresentationEventId);

        var course = await uow.Courses.GetByIdAsync(presentation.CourseId, ct)
            ?? throw new NotFoundException("Course", presentation.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            throw new UnauthorizedException("Only the teacher can publish.");

        // Compute deterministic hash of current marks state for dedupe
        var marks = (await uow.GetRepository<PresentationMark>()
            .FindAsync(m => m.PresentationEventId == command.PresentationEventId, ct))
            .OrderBy(m => m.StudentId)
            .ToList();

        var hashSource = string.Join("|",
            marks.Select(m => $"{m.StudentId}:{m.Marks}:{m.IsAbsent}"));
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashSource));
        var marksHash = Convert.ToHexString(hashBytes);

        var firstPublish = !presentation.IsPublished;
        var marksChanged = presentation.LastPublishedMarksHash != marksHash;
        var shouldNotify = firstPublish || marksChanged;

        presentation.Publish(marksHash);
        uow.GetRepository<PresentationEvent>().Update(presentation);
        await uow.SaveChangesAsync(ct);

        if (shouldNotify)
        {
            await NotifyEnrolledStudentsAsync(course.Id, course.Title, presentation.Title, ct);
        }

        return ApiResponse.Ok(shouldNotify
            ? "Published. Students notified."
            : "Published. No marks changed since last publish; nobody was notified again.");
    }

    /// <summary>
    /// Goes through SendNotificationCommand rather than emailing directly.
    /// The old version called IEmailService in a loop, which meant it ignored
    /// the student's notification preferences entirely and left no in-app
    /// record — a student who had switched email off still got mailed, and a
    /// student who only wanted in-app notifications got nothing at all.
    /// </summary>
    private async Task NotifyEnrolledStudentsAsync(
        Guid courseId, string courseTitle, string testTitle, CancellationToken ct)
    {
        try
        {
            var members = await uow.CourseMembers.GetByCourseAsync(courseId, ct);
            var recipients = members.Where(m => m.IsActive).ToList();

            if (recipients.Count == 0)
            {
                logger.LogInformation("No active recipients for presentation in course {CourseId}", courseId);
                return;
            }

            foreach (var m in recipients)
            {
                await sender.Send(new SendNotificationCommand(
                    UserId: m.UserId,
                    Title: $"Marks published in {courseTitle}",
                    Body: $"Your marks for \"{testTitle}\" are now available.",
                    Type: NotificationType.MarksPublished,
                    RedirectUrl: $"/courses/{courseId}/presentations"
                ), ct);
            }

            logger.LogInformation(
                "Marks-published notifications queued for {Count} recipients in course {CourseId}",
                recipients.Count, courseId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NotifyEnrolledStudentsAsync failed for course {CourseId}", courseId);
        }
    }
}