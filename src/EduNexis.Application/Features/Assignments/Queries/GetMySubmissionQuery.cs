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
            submission.Marks,
            submission.Feedback,
            submission.IsGraded));
    }
}
