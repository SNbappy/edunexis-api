using EduNexis.Application.Features.Announcements.Commands;

namespace EduNexis.Application.Features.Assignments.Queries;

/// <summary>Comments on one assignment, oldest first.</summary>
public record GetAssignmentCommentsQuery(
    Guid CourseId, Guid AssignmentId, Guid RequesterId
) : IQuery<ApiResponse<List<CommentDto>>>;

public sealed class GetAssignmentCommentsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetAssignmentCommentsQuery, ApiResponse<List<CommentDto>>>
{
    public async ValueTask<ApiResponse<List<CommentDto>>> Handle(
        GetAssignmentCommentsQuery query, CancellationToken ct)
    {
        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(query.AssignmentId, ct);

        if (assignment is null || assignment.CourseId != query.CourseId)
            return ApiResponse<List<CommentDto>>.Ok([]);

        var comments = await uow.GetRepository<AssignmentComment>()
            .FindAsync(c => c.AssignmentId == query.AssignmentId && !c.IsDeleted, ct);

        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct);
        var isTeacher = course?.TeacherId == query.RequesterId;

        // One profile lookup per distinct author, not per comment.
        var authors = new Dictionary<Guid, (string Name, string? Photo)>();
        var dtos = new List<CommentDto>();

        foreach (var c in comments.OrderBy(c => c.CreatedAt))
        {
            if (!authors.TryGetValue(c.AuthorId, out var author))
            {
                var user = await uow.Users.GetWithProfileAsync(c.AuthorId, ct);
                author = (user?.Profile?.FullName ?? "Unknown", user?.Profile?.ProfilePhotoUrl);
                authors[c.AuthorId] = author;
            }

            dtos.Add(new CommentDto(
                c.Id, c.AssignmentId, c.AuthorId,
                author.Name, author.Photo,
                c.Content, c.CreatedAt,
                CanDelete: isTeacher || c.AuthorId == query.RequesterId,
                CanEdit:   c.AuthorId == query.RequesterId,
                EditedAt:  c.UpdatedAt));
        }

        return ApiResponse<List<CommentDto>>.Ok(dtos);
    }
}
