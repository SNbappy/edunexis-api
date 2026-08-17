using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;


namespace EduNexis.Application.Features.Assignments.Commands;


/// <summary>One file being uploaded as part of a submission.</summary>
public record IncomingFile(Stream Stream, string FileName, long? SizeBytes);

public record SubmitAssignmentCommand(
    Guid AssignmentId,
    Guid StudentId,
    SubmissionType SubmissionType,
    string? TextContent,
    /// <summary>Every file the student attached. May be empty.</summary>
    IReadOnlyList<IncomingFile> Files,
    /// <summary>Every link the student attached. May be empty.</summary>
    IReadOnlyList<string> Links
) : ICommand<ApiResponse<SubmissionDto>>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var e = await uow.GetRepository<Assignment>().GetByIdAsync(AssignmentId, ct);
        return e?.CourseId;
    }
}


public sealed class SubmitAssignmentCommandValidator : AbstractValidator<SubmitAssignmentCommand>
{
    public SubmitAssignmentCommandValidator()
    {
        RuleFor(x => x.TextContent)
            .NotEmpty().When(x => x.SubmissionType == SubmissionType.Text)
            .WithMessage("Text content is required for text submissions.");
        RuleFor(x => x.Files)
            .Must(f => f is { Count: > 0 }).When(x => x.SubmissionType == SubmissionType.File)
            .WithMessage("At least one file is required for file submissions.");
        RuleFor(x => x.Links)
            .Must(l => l is { Count: > 0 }).When(x => x.SubmissionType == SubmissionType.Link)
            .WithMessage("At least one link is required for link submissions.");
    }
}


public sealed class SubmitAssignmentCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage,
    ISender sender
) : ICommandHandler<SubmitAssignmentCommand, ApiResponse<SubmissionDto>>
{
    public async ValueTask<ApiResponse<SubmissionDto>> Handle(
        SubmitAssignmentCommand command, CancellationToken ct)
    {
        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(command.AssignmentId, ct)
            ?? throw new NotFoundException("Assignment", command.AssignmentId);

        bool isLate = DateTime.UtcNow > assignment.Deadline;
        if (isLate && !assignment.AllowLateSubmission)
            return ApiResponse<SubmissionDto>.Fail("Deadline has passed. Late submissions not allowed.");

        var existing = await uow.GetRepository<AssignmentSubmission>()
            .FirstOrDefaultAsync(s =>
                s.AssignmentId == command.AssignmentId &&
                s.StudentId == command.StudentId, ct);

        // Upload every attached file. Done before touching the submission so a
        // storage failure leaves the previous submission untouched rather than
        // half-replaced.
        var uploaded = new List<(string Url, string FileName, long? Size)>();
        foreach (var f in command.Files ?? [])
        {
            var url = await storage.UploadAsync(
                f.Stream, f.FileName, $"submissions/{command.AssignmentId}", ct);
            uploaded.Add((url, f.FileName, f.SizeBytes));
        }

        var links = (command.Links ?? [])
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        // The legacy single-value columns mirror the first of each, so anything
        // still reading FileUrl/LinkUrl keeps behaving as before.
        var firstFileUrl = uploaded.FirstOrDefault().Url;
        var firstLinkUrl = links.FirstOrDefault();

        if (existing is not null)
        {
            existing.Update(command.SubmissionType, command.TextContent, firstFileUrl, firstLinkUrl);
            uow.GetRepository<AssignmentSubmission>().Update(existing);

            // Resubmitting replaces the attachment set — otherwise a student who
            // fixes one file ends up with both versions attached and the teacher
            // cannot tell which is current.
            var old = await uow.GetRepository<SubmissionAttachment>()
                .FindAsync(a => a.SubmissionId == existing.Id, ct);
            foreach (var a in old)
                uow.GetRepository<SubmissionAttachment>().Delete(a);
        }
        else
        {
            existing = AssignmentSubmission.Create(
                command.AssignmentId, command.StudentId,
                command.SubmissionType, command.TextContent,
                firstFileUrl, firstLinkUrl, isLate);
            await uow.GetRepository<AssignmentSubmission>().AddAsync(existing, ct);
        }

        await uow.SaveChangesAsync(ct);

        var order = 0;
        foreach (var (url, name, size) in uploaded)
        {
            await uow.GetRepository<SubmissionAttachment>().AddAsync(
                SubmissionAttachment.Create(
                    existing.Id, SubmissionAttachmentKind.File, url, name, size, order++), ct);
        }
        foreach (var link in links)
        {
            await uow.GetRepository<SubmissionAttachment>().AddAsync(
                SubmissionAttachment.Create(
                    existing.Id, SubmissionAttachmentKind.Link, link, null, null, order++), ct);
        }

        await uow.SaveChangesAsync(ct);

        var attachments = (await uow.GetRepository<SubmissionAttachment>()
                .FindAsync(a => a.SubmissionId == existing.Id, ct))
            .OrderBy(a => a.SortOrder)
            .Select(a => new SubmissionAttachmentDto(
                a.Id, a.Kind.ToString(), a.Url, a.FileName, a.FileSizeBytes))
            .ToList();

        // Fetch student full name from UserProfile
        var profile = await uow.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == command.StudentId, ct);
        var studentName = profile?.FullName ?? "Unknown";

        // Tell the teacher work has arrived. Without this the only way to know
        // anyone has submitted is to keep opening the assignment and counting.
        var course = await uow.Courses.GetByIdAsync(assignment.CourseId, ct);
        if (course is not null)
        {
            await sender.Send(new SendNotificationCommand(
                UserId: course.TeacherId,
                Title: $"New submission in {course.Title}",
                Body: isLate
                    ? $"{studentName} submitted \"{assignment.Title}\" (late)."
                    : $"{studentName} submitted \"{assignment.Title}\".",
                Type: NotificationType.SubmissionReceived,
                RedirectUrl: $"/courses/{course.Id}/assignments/{assignment.Id}"
            ), ct);
        }

        return ApiResponse<SubmissionDto>.Ok(new SubmissionDto(
            existing.Id, existing.AssignmentId, existing.StudentId,
            studentName, existing.SubmissionType.ToString(),
            existing.TextContent, existing.FileUrl, existing.LinkUrl,
            existing.SubmittedAt, existing.IsLate, existing.Marks,
            existing.Feedback, existing.IsGraded, attachments));
    }
}
