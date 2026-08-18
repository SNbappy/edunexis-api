using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Assignments.Queries;

public record GetMySubmissionQuery(Guid AssignmentId, Guid StudentId)
    : IQuery<ApiResponse<SubmissionDto>>;

public sealed class GetMySubmissionQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMySubmissionQuery, ApiResponse<SubmissionDto>>
{
    public async ValueTask<ApiResponse<SubmissionDto>> Handle(
        GetMySubmissionQuery query, CancellationToken ct)
    {
        var submission = await uow.GetRepository<AssignmentSubmission>()
            .FirstOrDefaultAsync(
                s => s.AssignmentId == query.AssignmentId &&
                     s.StudentId == query.StudentId, ct);

        if (submission is null)
            return ApiResponse<SubmissionDto>.Fail("No submission found.");

        var profile = await uow.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == query.StudentId, ct);
        var studentName = profile?.FullName ?? "Unknown";

        var attachments = (await uow.GetRepository<SubmissionAttachment>()
                .FindAsync(a => a.SubmissionId == submission.Id, ct))
            .OrderBy(a => a.SortOrder)
            .Select(a => new SubmissionAttachmentDto(
                a.Id, a.Kind.ToString(), a.Url, a.FileName, a.FileSizeBytes))
            .ToList();

        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(query.AssignmentId, ct);
        bool isPublished = assignment?.IsPublished == true;

        var marks = isPublished ? submission.Marks : null;
        var feedback = isPublished ? submission.Feedback : null;
        var isGraded = isPublished && submission.IsGraded;

        return ApiResponse<SubmissionDto>.Ok(new SubmissionDto(
            submission.Id,
            submission.AssignmentId,
            submission.StudentId,
            studentName,
            submission.SubmissionType.ToString(),
            submission.TextContent,
            submission.FileUrl,
            submission.LinkUrl,
            submission.SubmittedAt,
            submission.IsLate,
            marks,
            feedback,
            isGraded,
            attachments,
            submission.IsTurnedIn,
            submission.TurnedInAt,
            submission.IsAutoZero));
    }
}
