using EduNexis.Application.DTOs;


namespace EduNexis.Application.Features.Assignments.Commands;


public record SubmitAssignmentCommand(
    Guid AssignmentId,
    Guid StudentId,
    SubmissionType SubmissionType,
    string? TextContent,
    Stream? FileStream,
    string? FileName,
    string? LinkUrl
) : ICommand<ApiResponse<SubmissionDto>>;


public sealed class SubmitAssignmentCommandValidator : AbstractValidator<SubmitAssignmentCommand>
{
    public SubmitAssignmentCommandValidator()
    {
        RuleFor(x => x.TextContent)
            .NotEmpty().When(x => x.SubmissionType == SubmissionType.Text)
            .WithMessage("Text content is required for text submissions.");
        RuleFor(x => x.FileStream)
            .NotNull().When(x => x.SubmissionType == SubmissionType.File)
            .WithMessage("File is required for file submissions.");
        RuleFor(x => x.LinkUrl)
            .NotEmpty().When(x => x.SubmissionType == SubmissionType.Link)
            .WithMessage("Link URL is required for link submissions.");
    }
}


public sealed class SubmitAssignmentCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage
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

        string? fileUrl = null;
        if (command.SubmissionType == SubmissionType.File &&
            command.FileStream is not null && command.FileName is not null)
        {
            fileUrl = await storage.UploadAsync(
                command.FileStream, command.FileName,
                $"submissions/{command.AssignmentId}", ct);
        }

        if (existing is not null)
        {
            existing.Update(command.SubmissionType, command.TextContent, fileUrl, command.LinkUrl);
            uow.GetRepository<AssignmentSubmission>().Update(existing);
        }
        else
        {
            existing = AssignmentSubmission.Create(
                command.AssignmentId, command.StudentId,
                command.SubmissionType, command.TextContent,
                fileUrl, command.LinkUrl, isLate);
            await uow.GetRepository<AssignmentSubmission>().AddAsync(existing, ct);
        }

        await uow.SaveChangesAsync(ct);

        // ✅ Fetch student full name from UserProfile
        var profile = await uow.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == command.StudentId, ct);
        var studentName = profile?.FullName ?? "Unknown";

        return ApiResponse<SubmissionDto>.Ok(new SubmissionDto(
            existing.Id, existing.AssignmentId, existing.StudentId,
            studentName, existing.SubmissionType.ToString(),
            existing.TextContent, existing.FileUrl, existing.LinkUrl,
            existing.SubmittedAt, existing.IsLate, existing.Marks,
            existing.Feedback, existing.IsGraded));
    }
}
