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
        var announcements = await uow.GetRepository<Announcement>()
            .FindAsync(a => a.CourseId == query.CourseId && !a.IsDeleted, ct);

        var ids = announcements.Select(a => a.Id).ToHashSet();
        if (ids.Count == 0)
            return ApiResponse<List<CommentDto>>.Ok([]);

        var comments = await uow.GetRepository<AnnouncementComment>()
            .FindAsync(c => ids.Contains(c.AnnouncementId) && !c.IsDeleted, ct);

        var course = await uow.GetRepository<Course>().GetByIdAsync(query.CourseId, ct);
        var isTeacher = course?.TeacherId == query.RequesterId;

        // One profile lookup per distinct author rather than per comment: a
        // lively thread is mostly the same few people.
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
                c.Id, c.AnnouncementId, c.AuthorId,
                author.Name, author.Photo,
                c.Content, c.CreatedAt,
                CanDelete: isTeacher || c.AuthorId == query.RequesterId));
        }

        return ApiResponse<List<CommentDto>>.Ok(dtos);
    }
}
