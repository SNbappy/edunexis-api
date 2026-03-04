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
        var submissions = await uow.GetRepository<AssignmentSubmission>()
            .FindAsync(s => s.AssignmentId == query.AssignmentId, ct);

        var allProfiles = await uow.UserProfiles.GetAllAsync(ct);
        var profileMap = allProfiles.ToDictionary(p => p.UserId, p => p.FullName);

        var dtos = submissions.OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionDto(
                s.Id, s.AssignmentId, s.StudentId,
                profileMap.TryGetValue(s.StudentId, out var name) ? name : "Unknown",
                s.SubmissionType.ToString(),
                s.TextContent, s.FileUrl, s.LinkUrl,
                s.SubmittedAt, s.IsLate, s.Marks, s.Feedback, s.IsGraded))
            .ToList();

        return ApiResponse<List<SubmissionDto>>.Ok(dtos);
    }
}
