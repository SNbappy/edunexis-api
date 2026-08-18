using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Assignments.Queries;

public record GetSubmissionsQuery(Guid AssignmentId)
    : IQuery<ApiResponse<List<SubmissionDto>>>;

public sealed class GetSubmissionsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetSubmissionsQuery, ApiResponse<List<SubmissionDto>>>
{
    public async ValueTask<ApiResponse<List<SubmissionDto>>> Handle(
        GetSubmissionsQuery query, CancellationToken ct)
    {
        // Drafts are invisible to staff. A student still assembling work in the
        // hours before a deadline has not handed anything in, and a teacher must
        // not be able to read — let alone mark — an unfinished answer.
        var submissions = await uow.GetRepository<AssignmentSubmission>()
            .FindAsync(s => s.AssignmentId == query.AssignmentId && s.IsTurnedIn, ct);

        var allProfiles = await uow.UserProfiles.GetAllAsync(ct);
        var profileMap = allProfiles.ToDictionary(p => p.UserId, p => p.FullName);
        var photoMap = allProfiles.ToDictionary(p => p.UserId, p => p.ProfilePhotoUrl);

        // One query for every attachment in this assignment, grouped in memory —
        // a per-submission lookup would be one round trip per student.
        var submissionIds = submissions.Select(s => s.Id).ToHashSet();
        var attachmentMap = (await uow.GetRepository<SubmissionAttachment>()
                .FindAsync(a => submissionIds.Contains(a.SubmissionId), ct))
            .GroupBy(a => a.SubmissionId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<SubmissionAttachmentDto>)g
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new SubmissionAttachmentDto(
                        a.Id, a.Kind.ToString(), a.Url, a.FileName, a.FileSizeBytes))
                    .ToList());

        var dtos = submissions.OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionDto(
                s.Id, s.AssignmentId, s.StudentId,
                profileMap.TryGetValue(s.StudentId, out var name) ? name : "Unknown",
                s.SubmissionType.ToString(),
                s.TextContent, s.FileUrl, s.LinkUrl,
                s.SubmittedAt, s.IsLate, s.Marks, s.Feedback, s.IsGraded,
                attachmentMap.TryGetValue(s.Id, out var att) ? att : [],
                s.IsTurnedIn, s.TurnedInAt, s.IsAutoZero,
                photoMap.TryGetValue(s.StudentId, out var photo) ? photo : null))
            .ToList();

        return ApiResponse<List<SubmissionDto>>.Ok(dtos);
    }
}
