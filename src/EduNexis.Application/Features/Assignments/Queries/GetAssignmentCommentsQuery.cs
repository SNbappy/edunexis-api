using EduNexis.Application.Features.Announcements.Commands;

using EduNexis.Application.Abstractions;

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

        // Membership check first. This took a requester id and never used it to
        // decide anything, so any signed-in user with an assignment id could
        // read the whole class discussion under it.
        var course = await uow.Courses.GetByIdAsync(query.CourseId, ct);
        if (course is null) return ApiResponse<List<CommentDto>>.Ok([]);

        var isTeacher = await CourseAccess.IsTeacherAsync(uow, course, query.RequesterId, ct);
        if (!isTeacher)
        {
            var member = await uow.CourseMembers.GetMemberAsync(query.CourseId, query.RequesterId, ct);
            if (member is null)
                return ApiResponse<List<CommentDto>>.Fail("You are not a member of this course.");
        }

        var rawComments = await uow.GetRepository<AssignmentComment>()
            .FindAsync(c => c.AssignmentId == query.AssignmentId && !c.IsDeleted, ct);

        // Filter out any orphan replies whose parent is deleted/missing
        var activeIds = rawComments.Select(c => c.Id).ToHashSet();
        var comments = rawComments.Where(c => c.ParentCommentId is null || activeIds.Contains(c.ParentCommentId.Value)).ToList();

        // One profile lookup per distinct author, not per comment.
        var authors = new Dictionary<Guid, (string Name, string? Photo)>();
        var dtos = new List<CommentDto>();

        // Who each replied-to comment belongs to, so a reply can name it.
        var byId = comments.ToDictionary(c => c.Id, c => c.AuthorId);
        var parentAuthorName = new Dictionary<Guid, string>();

        foreach (var c in comments.OrderBy(c => c.CreatedAt))
        {
            if (!authors.TryGetValue(c.AuthorId, out var author))
            {
                var user = await uow.Users.GetWithProfileAsync(c.AuthorId, ct);
                author = (user?.Profile?.FullName ?? "Unknown", user?.Profile?.ProfilePhotoUrl);
                authors[c.AuthorId] = author;
            }

            if (c.ParentCommentId is Guid parentId
                && !parentAuthorName.ContainsKey(parentId)
                && byId.TryGetValue(parentId, out var parentAuthorId))
            {
                if (!authors.TryGetValue(parentAuthorId, out var pa))
                {
                    var pu = await uow.Users.GetWithProfileAsync(parentAuthorId, ct);
                    pa = (pu?.Profile?.FullName ?? "Unknown", pu?.Profile?.ProfilePhotoUrl);
                    authors[parentAuthorId] = pa;
                }
                parentAuthorName[parentId] = pa.Name;
            }

            dtos.Add(new CommentDto(
                c.Id, c.AssignmentId, c.AuthorId,
                author.Name, author.Photo,
                c.Content, c.CreatedAt,
                CanDelete: isTeacher || c.AuthorId == query.RequesterId,
                CanEdit:   c.AuthorId == query.RequesterId,
                EditedAt:  c.UpdatedAt,
                ParentCommentId: c.ParentCommentId,
                ReplyToName: c.ParentCommentId is Guid pid && parentAuthorName.TryGetValue(pid, out var replyTo)
                    ? replyTo
                    : null));
        }

        return ApiResponse<List<CommentDto>>.Ok(dtos);
    }
}
