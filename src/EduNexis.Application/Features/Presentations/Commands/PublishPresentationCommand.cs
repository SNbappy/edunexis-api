using System.Security.Cryptography;
using System.Text;
using EduNexis.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EduNexis.Application.Features.Presentations.Commands;

public record PublishPresentationCommand(
    Guid PresentationEventId,
    Guid TeacherId
) : ICommand<ApiResponse>;

public sealed class PublishPresentationCommandHandler(
    IUnitOfWork uow,
    IEmailService emailService,
    IEmailTemplateBuilder templateBuilder,
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

        if (course.TeacherId != command.TeacherId)
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
            : "Published. No marks changed since last publish; no email sent.");
    }

    private async Task NotifyEnrolledStudentsAsync(
        Guid courseId, string courseTitle, string testTitle, CancellationToken ct)
    {
        try
        {
            var members = await uow.CourseMembers.GetByCourseAsync(courseId, ct);
            var recipients = members
                .Where(m => m.IsActive && m.User is not null && !string.IsNullOrWhiteSpace(m.User.Email))
                .Select(m => m.User!.Email)
                .Distinct()
                .ToList();

            if (recipients.Count == 0)
            {
                logger.LogInformation("No active recipients for presentation in course {CourseId}", courseId);
                return;
            }

            var bodyHtml =
                $"<p>Your marks for <strong>{System.Net.WebUtility.HtmlEncode(testTitle)}</strong> in <strong>{System.Net.WebUtility.HtmlEncode(courseTitle)}</strong> are now available.</p>" +
                "<p>Log in to EduNexis to view your result.</p>" +
                "<p style=\"color:#78716c;font-size:13px;\">If you are not enrolled in this course, please ignore this email.</p>";

            var html = templateBuilder.Build("Marks published", bodyHtml);
            var subject = $"Marks published: {testTitle}";

            foreach (var email in recipients)
            {
                try
                {
                    await emailService.SendAsync(email, subject, html, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send marks-published email to {Email}", email);
                }
            }

            logger.LogInformation("Marks-published emails dispatched to {Count} recipients for course {CourseId}", recipients.Count, courseId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NotifyEnrolledStudentsAsync failed for course {CourseId}", courseId);
        }
    }
}