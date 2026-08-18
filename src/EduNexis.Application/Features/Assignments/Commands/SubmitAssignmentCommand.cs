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
    IReadOnlyList<string> Links,
    /// <summary>
    /// Ids of attachments already on the submission that the student is keeping.
    /// Anything on the submission but absent from this list is removed. Null means
    /// "the client did not say" — treated as keep everything, so an older client
    /// that knows nothing about this field cannot wipe a student's files.
    /// </summary>
    IReadOnlyList<Guid>? KeepAttachmentIds = null
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
        // On an update the student may be keeping what is already turned in and
        // uploading nothing new, so a kept attachment satisfies these rules too.
        static bool KeepsSomething(SubmitAssignmentCommand x) =>
            x.KeepAttachmentIds is { Count: > 0 };

        RuleFor(x => x.TextContent)
            .NotEmpty()
            .When(x => x.SubmissionType == SubmissionType.Text && !KeepsSomething(x))
            .WithMessage("Text content is required for text submissions.");
        RuleFor(x => x.Files)
            .Must(f => f is { Count: > 0 })
            .When(x => x.SubmissionType == SubmissionType.File && !KeepsSomething(x))
            .WithMessage("At least one file is required for file submissions.");
        RuleFor(x => x.Links)
            .Must(l => l is { Count: > 0 })
            .When(x => x.SubmissionType == SubmissionType.Link && !KeepsSomething(x))
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

        // Attachments the student is keeping from a previous submission. An update
        // adds to what is already turned in; only what the student explicitly
        // removed is deleted. Replacing the whole set used to silently drop every
        // file a student did not re-upload.
        var kept = new List<SubmissionAttachment>();

        if (existing is not null)
        {
            var old = (await uow.GetRepository<SubmissionAttachment>()
                .FindAsync(a => a.SubmissionId == existing.Id, ct))
                .OrderBy(a => a.SortOrder)
                .ToList();

            foreach (var a in old)
            {
                if (command.KeepAttachmentIds is null || command.KeepAttachmentIds.Contains(a.Id))
                    kept.Add(a);
                else
                    uow.GetRepository<SubmissionAttachment>().Delete(a);
            }

            // The legacy single-value columns mirror the first of each across the
            // final set, kept attachments included.
            var keptFile = kept.FirstOrDefault(a => a.Kind == SubmissionAttachmentKind.File)?.Url;
            var keptLink = kept.FirstOrDefault(a => a.Kind == SubmissionAttachmentKind.Link)?.Url;

            existing.Update(
                command.SubmissionType,
                command.TextContent,
                keptFile ?? firstFileUrl,
                keptLink ?? firstLinkUrl);
            uow.GetRepository<AssignmentSubmission>().Update(existing);
        }
        else
        {
            // Attaching work is not handing it in. A new submission starts as a
            // draft that only the student can see; TurnInAssignmentCommand is
            // what makes it visible to the teacher.
            existing = AssignmentSubmission.Create(
                command.AssignmentId, command.StudentId,
                command.SubmissionType, command.TextContent,
                firstFileUrl, firstLinkUrl, isLate,
                isTurnedIn: false);
            await uow.GetRepository<AssignmentSubmission>().AddAsync(existing, ct);
        }

        await uow.SaveChangesAsync(ct);

        // New attachments sort after whatever was kept.
        var order = kept.Count == 0 ? 0 : kept.Max(a => a.SortOrder) + 1;
        foreach (var (url, name, size) in uploaded)
        {
            await uow.GetRepository<SubmissionAttachment>().AddAsync(
                SubmissionAttachment.Create(
                    existing.Id, SubmissionAttachmentKind.File, url, name, size, order++), ct);
        }
        foreach (var link in links)
        {
            // A link the student already has attached should not be duplicated.
            if (kept.Any(a => a.Kind == SubmissionAttachmentKind.Link && a.Url == link)) continue;
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

        // Tell the teacher work has arrived — but only once it is actually
        // handed in. Announcing every saved draft would notify the teacher
        // repeatedly for work they still cannot see.
        var course = await uow.Courses.GetByIdAsync(assignment.CourseId, ct);
        if (course is not null && existing.IsTurnedIn)
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
            existing.Feedback, existing.IsGraded, attachments,
            existing.IsTurnedIn, existing.TurnedInAt, existing.IsAutoZero));
    }
}
