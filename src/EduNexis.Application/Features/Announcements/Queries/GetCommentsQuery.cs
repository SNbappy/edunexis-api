using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Announcements.Queries;

using EduNexis.Application.Features.Announcements.Commands;

/// <summary>
/// Comments on every announcement in a course, in one round trip.
///
/// Fetched per course rather than per announcement so the stream does not fire
/// one request per card — a course with thirty announcements would otherwise
/// open thirty connections on load.
/// </summary>
public record GetCommentsQuery(Guid CourseId, Guid RequesterId)
    : IQuery<ApiResponse<List<CommentDto>>>;

public sealed class GetCommentsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetCommentsQuery, ApiResponse<List<CommentDto>>>
{
    public async ValueTask<ApiResponse<List<CommentDto>>> Handle(
        GetCommentsQuery query, CancellationToken ct)
    {
        // Membership check first.
        //
        // This query took a course id and a requester and never compared them:
        // any signed-in user who knew or guessed a course id could read every
        // class discussion in it, including a teacher's answers to individual
        // students. Reading a thread requires being in the course, exactly as
        // writing to one already did.
        var course = await uow.GetRepository<Course>().GetByIdAsync(query.CourseId, ct);
        if (course is null) return ApiResponse<List<CommentDto>>.Ok([]);

        var isTeacher = await CourseAccess.IsTeacherAsync(uow, course, query.RequesterId, ct);
        if (!isTeacher)
        {
            var member = await uow.CourseMembers.GetMemberAsync(query.CourseId, query.RequesterId, ct);
            if (member is null)
                return ApiResponse<List<CommentDto>>.Fail("You are not a member of this course.");
        }

        var announcements = await uow.GetRepository<Announcement>()
            .FindAsync(a => a.CourseId == query.CourseId && !a.IsDeleted, ct);

        var ids = announcements.Select(a => a.Id).ToHashSet();
        if (ids.Count == 0)
            return ApiResponse<List<CommentDto>>.Ok([]);

        var rawComments = await uow.GetRepository<AnnouncementComment>()
            .FindAsync(c => ids.Contains(c.AnnouncementId) && !c.IsDeleted, ct);

        // Filter out any orphan replies whose parent is deleted/missing
        var activeIds = rawComments.Select(c => c.Id).ToHashSet();
        var comments = rawComments.Where(c => c.ParentCommentId is null || activeIds.Contains(c.ParentCommentId.Value)).ToList();

        // One profile lookup per distinct author rather than per comment: a
        // lively thread is mostly the same few people.
        var authors = new Dictionary<Guid, (string Name, string? Photo)>();
        var dtos = new List<CommentDto>();

        // Author of each comment that has been replied to, so a reply can name
        // who it answers without the client resolving it from the flat list.
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
                c.Id, c.AnnouncementId, c.AuthorId,
                author.Name, author.Photo,
                c.Content, c.CreatedAt,
                // Teacher moderates (delete anything); only the author rewrites.
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
