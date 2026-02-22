using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Assignments.Queries;

public record GetAssignmentsQuery(Guid CourseId) : IQuery<ApiResponse<List<AssignmentDto>>>;

public sealed class GetAssignmentsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetAssignmentsQuery, ApiResponse<List<AssignmentDto>>>
{
    public async ValueTask<ApiResponse<List<AssignmentDto>>> Handle(
        GetAssignmentsQuery query, CancellationToken ct)
    {
        var assignments = await uow.GetRepository<Assignment>()
            .FindAsync(a => a.CourseId == query.CourseId, ct);

        var dtos = new List<AssignmentDto>();
        foreach (var a in assignments.OrderByDescending(a => a.CreatedAt))
        {
            var subCount = (await uow.GetRepository<AssignmentSubmission>()
                .FindAsync(s => s.AssignmentId == a.Id, ct)).Count();
            dtos.Add(new AssignmentDto(
                a.Id, a.CourseId, a.Title, a.Instructions, a.Deadline,
                a.AllowLateSubmission, a.MaxMarks, a.RubricNotes,
                a.ReferenceFileUrl, a.IsOpen(), subCount, a.CreatedAt));
        }

        return ApiResponse<List<AssignmentDto>>.Ok(dtos);
    }
}
